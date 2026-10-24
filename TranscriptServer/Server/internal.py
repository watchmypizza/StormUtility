
from flask import Flask, request, jsonify, render_template, url_for
import dotenv, os, sqlite3, requests

dotenv.load_dotenv(".config")
token = os.getenv("DISCORD_TOKEN")

app = Flask(__name__)

members = {}

ADMINS = [1116315001330880602, 845103036317433867, 806333974795386923]

QUERIED_USERIDS = {}

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

def readDatabase(member_id):
    try:
        with sqlite3.connect("TranscriptServer/Server/static/test.db") as conn:
            res = conn.execute(
                "SELECT * FROM test WHERE id = ?",
                (int(member_id),)
            )
            row = res.fetchone()
            print(row)
            return row if row is not None else []

    except (ValueError, sqlite3.Error) as e:
        print(e)
        return []

@app.post("/profile")
def profile():
    data = request.get_json(silent=True)

    if data == None:
        return jsonify("Expected { id: integer } .")

    try:
        id = str(data["id"])
    except KeyError:
        return jsonify("Invalid json data.")

    obj = readDatabase(id)

    rank = "Expendable Asset"

    if int(obj[2]) >= 500:
       rank = "Corporate Asset"
    elif int(obj[2]) >= 1000:
       rank = "Essential Asset"
    elif int(obj[2]) >= 2000:
       rank = "Irreplaceable Asset"
    elif int(obj[2]) >= 5000:
       rank = "VIP"
    elif int(obj[2]) >= 10000:
       rank = "MVP"
    elif int(obj[2]) >= 50000:
       rank = "Unmatched"

    if int(obj[0]) in ADMINS:
       rank = "Administrator"

    pfp = extractUserProfilePicture(int(obj[0]))

    member = {
        "id": str(obj[0]),
        "name": obj[1],
        "pfp": pfp,
        "messages": obj[2],
        "role": {
            "rank": rank,
            "image_path": rank.strip().replace(" ", "").lower() + ".png"
        }
    }

    html = render_template("membertemplate.html", member=member)
    url = url_for("view_member", member_id=str(id), _external=True)

    members[str(id)] = member

    return jsonify({
        "html": html,
        "url": url
    })

@app.get("/user/<member_id>")
def view_member(member_id):
    member = members.get(member_id)

    if member is None:
        return jsonify({"error": "Member not found"}), 404

    return render_template("membertemplate.html", member=member)

if __name__ == "__main__":
    app.run(port=3001, debug=True)
