import unicodedata

from flask import Flask, request, jsonify, render_template
from datetime import datetime, timezone
from html import escape
from urllib.parse import urlparse
from markupsafe import Markup
import re
import secrets
import hashlib
import time
import hmac
import os
import requests
import json
import dotenv

app = Flask(__name__)

print("--- FLASK PATH DIAGNOSTICS ---")
print("Current Working Directory (CWD):", os.getcwd())
print("Flask App Root Path:", app.root_path)
print("Expected Static Folder Location:", app.static_folder)
print("------------------------------")

dotenv.load_dotenv(".config")
token = os.getenv("DISCORD_TOKEN")
BOT_SECRET = os.getenv("NONCE")

used_nonces = {}

NONCE_EXPIRY = 10
TRANSCRIPT_EXPIRY = 10 * 60
TRANSCRIPTS_FOLDER = "transcripts"

app.jinja_env.trim_blocks = True
app.jinja_env.lstrip_blocks = True

URL_RE = re.compile(r'(https?://[^\s<>"]+)')
IMAGE_EXTENSIONS = (".png", ".jpg", ".jpeg", ".gif", ".webp")

QUERIED_USERIDS = {}

def replace_unicode(str: str) -> str:
    cleaned_lines = []

    for line in str.splitlines():
        line = line.strip()

        line = unicodedata.normalize("NFKD", line)
        line = "".join(c for c in line if not unicodedata.combining(c))

        line = "".join(c for c in line if c.isprintable())

        cleaned_lines.append(line)

    return "\n".join(cleaned_lines)


def read_tsk(file_path):
    if not os.path.isfile(file_path):
        raise FileNotFoundError(f"TSK file not found: {file_path}")

    with open(file_path, "r", encoding="utf-8") as file:
        lines = file.read().splitlines()

    if not lines or lines[0].strip() != "BEGIN":
        raise ValueError("Expected BEGIN")

    if lines[-1].strip() != "END":
        raise ValueError("Expected END")

    messages = []

    i = 1

    while i < len(lines) - 1:
        if not lines[i].strip():
            i += 1
            continue

        message = {}

        while i < len(lines) - 1:
            line = lines[i]

            if not line.strip():
                i += 1
                break

            if ":" not in line:
                raise ValueError(
                    f"Malformed line {i + 1}: {line}"
                )

            key, value = line.split(":", 1)

            key = key.strip()
            value = value.strip()

            if value == '"""':
                i += 1

                content = []

                while i < len(lines):

                    if lines[i] == '"""':
                        break

                    content.append(lines[i])
                    i += 1


                if i >= len(lines):
                    raise ValueError(
                        f"Unclosed multiline string for {key}"
                    )

                message[key] = "\n".join(content)

                i += 1
                continue

            message[key] = value
            i += 1

        if not message:
            continue

        if "Embed" not in message:
            raise ValueError(
                "Missing Embed property."
            )

        if "UserId" not in message:
            raise ValueError(
                "Missing UserId"
            )

        if "Username" not in message:
            raise ValueError(
                "Missing Username"
            )

        if "Content" not in message:
            raise ValueError(
                "Missing Content"
            )

        if "Timestamp" not in message:
            raise ValueError(
                "Missing Timestamp"
            )

        try:
            user_id = int(message["UserId"])
        except ValueError:
            raise ValueError(
                f"Invalid UserId: {message['UserId']}"
            )

        if message.get("Embed"):
            embed = json.loads(message["Embed"])
        else:
            embed = None

        is_bot_str = message.get("IsBot", "false").strip().lower()
        is_bot_bool = is_bot_str == "true"

        cleaned_content = replace_unicode(message["Content"])

        messages.append(
            {
                "userId": user_id,
                "username": message["Username"],
                "content": message["Content"],
                "timestamp": message["Timestamp"],
                "messageId": message.get("MessageId"),
                "embed": embed,
                "isBot": is_bot_bool,
            }
        )

    return messages

def extractUserProfilePicture(userId):
    headers = {"Authorization": f"Bot {token}"}

    cached = QUERIED_USERIDS.get(userId)

    if cached:
        return cached

    try:
        response = requests.get(
            f"https://discord.com/api/v9/users/{userId}",
            headers=headers,
            timeout=10
        )

        if not response.ok:
            print(
                f"Discord API error {response.status_code}: "
                f"{response.text}"
            )
            return "https://cdn.discordapp.com/embed/avatars/1.png"

        data = response.json()
        avatar = data.get("avatar")

        if not avatar:
            return "https://cdn.discordapp.com/embed/avatars/1.png"

        QUERIED_USERIDS[userId] = f"https://cdn.discordapp.com/avatars/{userId}/{avatar}.png"

        return (
            f"https://cdn.discordapp.com/avatars/"
            f"{userId}/{avatar}.png"
        )

    except (requests.RequestException, ValueError) as e:
        print(
            f"Failed to get Discord avatar for {userId}: {e}"
        )

        QUERIED_USERIDS[userId] = "https://cdn.discordapp.com/embed/avatars/1.png"
        return "https://cdn.discordapp.com/embed/avatars/1.png"

def create_nonce():
    nonce = secrets.token_urlsafe(32)

    used_nonces[nonce] = time.time() + NONCE_EXPIRY

    return nonce

def consume_nonce(nonce):
    expiry = used_nonces.pop(nonce, None)

    if expiry is None:
        return False

    if time.time() >= expiry:
        return False

    return True

def verify_bot_request(nonce, timestamp, signature):
    try:
        timestamp = int(timestamp)
    except (TypeError, ValueError):
        return False

    if abs(time.time() - timestamp) > 60:
        return False

    message = f"{nonce}.{timestamp}"

    expected_signature = hmac.new(
        BOT_SECRET.encode(),
        message.encode(),
        hashlib.sha256
    ).hexdigest()

    return hmac.compare_digest(
        signature,
        expected_signature
    )

def embed_url(url):
    parsed = urlparse(url)
    host = parsed.netloc.lower()
    path = parsed.path.lower()

    if (
        host in ("cdn.discordapp.com", "media.discord.net")
        and "/attachments/" in path
        and path.endswith(IMAGE_EXTENSIONS)
    ):
        safe_url = escape(url)

        return Markup(
            f'<a href="{safe_url}" target="_blank" '
            f'rel="noopener noreferrer">'
            f'<img class="msg-attachment" '
            f'src="{safe_url}" alt="attachment" loading="lazy">'
            f'</a>'
        )

    if host == "medal.tv":
        clip_match = re.search(
            r"/clips?/([A-Za-z0-9]+)",
            parsed.path
        )

        if clip_match:
            clip_id = clip_match.group(1)

            game_match = re.search(
                r"/games/([^/]+)/",
                parsed.path
            )

            game = (
                game_match.group(1)
                if game_match
                else None
            )

            embed_path = (
                f"/games/{game}/clip/{clip_id}"
                if game
                else f"/clip/{clip_id}"
            )

            embed_src = escape(
                f"https://medal.tv{embed_path}"
            )

            return Markup(
                f'<div class="medal-embed">'
                f'<iframe src="{embed_src}" '
                f'allowfullscreen scrolling="no" '
                f'allow="encrypted-media *; fullscreen *;">'
                f'</iframe>'
                f'</div>'
            )

    safe_url = escape(url)

    return Markup(
        f'<a href="{safe_url}" target="_blank" '
        f'rel="noopener noreferrer">{safe_url}</a>'
    )

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

        pieces.append(
            escape(text[last_end:start])
        )

        pieces.append(
            embed_url(url)
        )

        if trailing:
            pieces.append(
                escape(trailing)
            )

        last_end = end

    pieces.append(
        escape(text[last_end:])
    )

    return Markup("".join(pieces))

app.jinja_env.filters["render_content"] = render_content

sessions = {}

def hash_password(password):
    return hashlib.sha256(
        password.encode("utf-8")
    ).hexdigest()

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

    delta_days = (
        now.date() - dt.date()
    ).days

    clock = format_clock(dt)

    if delta_days == 0:
        return f"Today at {clock}"

    if delta_days == 1:
        return f"Yesterday at {clock}"

    return (
        f"{dt.month}/{dt.day}/{dt.year} "
        f"{clock}"
    )

def format_date_label(dt):
    if dt is None:
        return ""

    return (
        f"{dt.strftime('%B')} "
        f"{dt.day}, {dt.year}"
    )

def get_transcript(ticket_id):
    normalized = ticket_id.replace("\\", "/")

    if normalized.startswith(f"{TRANSCRIPTS_FOLDER}/"):
        normalized = normalized[len(TRANSCRIPTS_FOLDER) + 1:]

    if normalized.endswith(".tsk"):
        normalized = normalized[:-4]

    file_path = os.path.join(TRANSCRIPTS_FOLDER, f"{normalized}.tsk")

    try:
        messages = read_tsk(file_path)
    except (FileNotFoundError, ValueError) as e:
        print(f"Failed to read transcript: {e}")
        return None

    for message in messages:
        user_id = message["userId"]

        message["avatar"] = (
            extractUserProfilePicture(user_id)
        )

    messages.sort(
        key=lambda message: message["timestamp"]
    )

    for message in messages:
        dt = parse_timestamp(
            message["timestamp"]
        )

        message["date_key"] = (
            dt.strftime("%Y-%m-%d")
            if dt
            else ""
        )

        message["date_label"] = (
            format_date_label(dt)
        )

        message["timestamp"] = (
            format_timestamp(dt)
        )

    return messages

@app.post("/transcript/createInterface")
def create_interface():
    data = request.get_json(silent=True)

    if not data:
        return jsonify({
            "error": "Invalid JSON body"
        }), 400

    nonce = data.get("nonce")
    timestamp = data.get("timestamp")
    signature = data.get("signature")

    if not nonce or not timestamp or not signature:
        token = secrets.token_urlsafe(32)

        sessions[token] = {
            "ticket_id": "getout",
            "expires": (
                time.time() + TRANSCRIPT_EXPIRY
            )
        }

        return jsonify({
            "url": (
                "https://ilikepeanutbutter.duckdns.org/"
                f"ticket?id={token}"
            ),
            "expires": TRANSCRIPT_EXPIRY
        })

    if not verify_bot_request(
        nonce,
        timestamp,
        signature
    ):
        token = secrets.token_urlsafe(32)

        sessions[token] = {
            "ticket_id": "getout",
            "expires": (
                time.time() + TRANSCRIPT_EXPIRY
            )
        }

        return jsonify({
            "url": (
                "https://ilikepeanutbutter.duckdns.org/"
                f"ticket?id={token}"
            ),
            "expires": TRANSCRIPT_EXPIRY
        })

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
        "expires": (
            time.time() + TRANSCRIPT_EXPIRY
        )
    }

    return jsonify({
        "url": (
            "https://ilikepeanutbutter.duckdns.org/"
            f"ticket?id={token}"
        ),
        "expires": TRANSCRIPT_EXPIRY
    })


@app.get("/ticket")
def ticket():
    token = request.args.get("id")

    if not token:
        return render_template(
            "403page.html"
        ), 403

    session = sessions.get(token)

    if not session:
        return render_template(
            "403page.html"
        ), 403

    if time.time() > session["expires"]:
        del sessions[token]

        return render_template(
            "403page.html"
        ), 403

    if session["ticket_id"] == "getout":
        return render_template(
            "404page.html"
        ), 404

    messages = get_transcript(
        session["ticket_id"]
    )

    if messages is None:
        return render_template(
            "403page.html"
        ), 403

    return render_template(
        "ticket.html",
        channel_name=session["ticket_id"],
        messages=messages,
    )


if __name__ == "__main__":
    app.run(
        host="127.0.0.1",
        port=3000
    )
