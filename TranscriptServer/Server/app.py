from flask import Flask, request, jsonify, render_template
from datetime import datetime, timezone
from html import escape
from urllib.parse import urlparse
from markupsafe import Markup
import re
import secrets
import hashlib
import time
import json

app = Flask(__name__)

app.jinja_env.trim_blocks = True
app.jinja_env.lstrip_blocks = True

URL_RE = re.compile(r'(https?://[^\s<>"]+)')
IMAGE_EXTENSIONS = (".png", ".jpg", ".jpeg", ".gif", ".webp")


def embed_url(url):
    parsed = urlparse(url)
    host = parsed.netloc.lower()
    path = parsed.path.lower()

    if host in ("cdn.discordapp.com", "media.discordapp.net") and "/attachments/" in path:
        if path.endswith(IMAGE_EXTENSIONS):
            safe_url = escape(url)
            return Markup(
                f'<a href="{safe_url}" target="_blank" rel="noopener noreferrer">'
                f'<img class="msg-attachment" src="{safe_url}" alt="attachment" loading="lazy">'
                f'</a>'
            )

    if host == "medal.tv":
        clip_match = re.search(r"/clips?/([A-Za-z0-9]+)", parsed.path)

        if clip_match:
            clip_id = clip_match.group(1)
            game_match = re.search(r"/games/([^/]+)/", parsed.path)
            game = game_match.group(1) if game_match else None

            embed_path = f"/games/{game}/clip/{clip_id}" if game else f"/clip/{clip_id}"
            embed_src = escape(f"https://medal.tv{embed_path}")

            return Markup(
                f'<div class="medal-embed">'
                f'<iframe src="{embed_src}" allowfullscreen scrolling="no" '
                f'allow="encrypted-media *; fullscreen *;"></iframe>'
                f'</div>'
            )

    safe_url = escape(url)
    return Markup(f'<a href="{safe_url}" target="_blank" rel="noopener noreferrer">{safe_url}</a>')


def render_content(text):

    if not text:
        return Markup("")

    pieces = []
    last_end = 0

    for match in URL_RE.finditer(text):
        start, end = match.span()
        url = match.group(1)

        trailing = ""
        while url and url[-1] in ").,!?;:":
            trailing = url[-1] + trailing
            url = url[:-1]
            end -= 1

        pieces.append(escape(text[last_end:start]))
        pieces.append(embed_url(url))

        if trailing:
            pieces.append(escape(trailing))

        last_end = end

    pieces.append(escape(text[last_end:]))

    return Markup("".join(pieces))


app.jinja_env.filters["render_content"] = render_content

sessions = {}

TRANSCRIPT_EXPIRY = 10 * 60
TRANSCRIPTS_FILE = "transcripts.json"


def hash_password(password):
    return hashlib.sha256(
        password.encode("utf-8")
    ).hexdigest()


def load_transcripts():
    with open(TRANSCRIPTS_FILE, "r", encoding="utf-8") as file:
        return json.load(file)


def parse_timestamp(raw):
    if not raw:
        return None

    try:
        return datetime.fromisoformat(
            raw.replace("Z", "+00:00")
        )
    except ValueError:
        return None


def format_clock(dt):
    hour = dt.hour % 12
    hour = 12 if hour == 0 else hour

    return f"{hour}:{dt.strftime('%M %p')}"


def format_timestamp(dt):
    if dt is None:
        return ""

    now = datetime.now(timezone.utc)

    delta_days = (now.date() - dt.date()).days
    clock = format_clock(dt)

    if delta_days == 0:
        return f"Today at {clock}"
    elif delta_days == 1:
        return f"Yesterday at {clock}"
    else:
        return f"{dt.month}/{dt.day}/{dt.year} {clock}"


def format_date_label(dt):
    if dt is None:
        return ""

    return f"{dt.strftime('%B')} {dt.day}, {dt.year}"


def get_transcript(ticket_id):
    data = load_transcripts()

    transcripts = data.get("transcripts", {})

    ticket = transcripts.get(ticket_id)

    if ticket is None:
        return None

    messages = []

    for user_id, user_data in ticket.items():

        for message in user_data.get("messages", []):

            messages.append({
                "username": message.get("username", "Unknown User"),
                "content": message.get("content", ""),
                "raw_timestamp": message.get("timestamp", ""),
                "messageId": message.get("messageId", "")
            })

    messages.sort(
        key=lambda message: message["raw_timestamp"]
    )

    for message in messages:
        dt = parse_timestamp(message.pop("raw_timestamp"))

        message["date_key"] = dt.strftime("%Y-%m-%d") if dt else ""
        message["date_label"] = format_date_label(dt)
        message["timestamp"] = format_timestamp(dt)

    return messages


@app.post("/transcript/createInterface")
def create_interface():

    data = request.get_json(silent=True)

    if not data:
        return jsonify({
            "error": "Invalid JSON body"
        }), 400

    ticket_id = data.get("ticketId")

    if not ticket_id:
        return jsonify({
            "error": "Missing ticketId"
        }), 400

    messages = get_transcript(ticket_id)

    if messages is None:
        return jsonify({
            "error": "Transcript not found",
            "ticketId": ticket_id
        }), 404

    token = secrets.token_urlsafe(32)

    sessions[token] = {
        "ticket_id": ticket_id,
        "expires": time.time() + TRANSCRIPT_EXPIRY
    }

    return jsonify({
        "url": f"https://ilikepeanutbutter.duckdns.org/ticket?id={token}",
        "expires": TRANSCRIPT_EXPIRY
    })

@app.get("/ticket")
def ticket():

    token = request.args.get("id")

    if not token:
        return "Invalid ticket link.", 400

    session = sessions.get(token)

    if not session:
        return "Ticket link expired or invalid.", 404

    if time.time() > session["expires"]:
        del sessions[token]

        return "Ticket link expired.", 410

    messages = get_transcript(
        session["ticket_id"]
    )

    if messages is None:
        return "Transcript no longer exists.", 404

    return render_template(
        "ticket.html",
        channel_name=session["ticket_id"],
        messages=messages
    )


if __name__ == "__main__":
    app.run(
        host="127.0.0.1",
        port=3000
    )