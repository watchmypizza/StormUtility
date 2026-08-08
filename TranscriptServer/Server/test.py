import os
import secrets
import time
import hmac
import hashlib
import base64
import requests
from dotenv import load_dotenv

load_dotenv("../../.config")

BOT_SECRET = os.environ.get("NONCE")

if not BOT_SECRET:
    raise RuntimeError("NONCE was not loaded from .config")

nonce = base64.b64encode(
    secrets.token_bytes(32)
).decode("ascii")

timestamp = int(time.time())

message = f"{nonce}.{timestamp}"

signature = hmac.new(
    BOT_SECRET.encode("utf-8"),
    message.encode("utf-8"),
    hashlib.sha256
).hexdigest()

payload = {
    "ticketId": "test-0",
    "nonce": nonce,
    "timestamp": timestamp,
    "signature": signature
}

print("Nonce:", nonce)
print("Timestamp:", timestamp)
print("Signature:", signature)
print("Ticket:", payload["ticketId"])

response = requests.post(
    "http://127.0.0.1:3000/transcript/createInterface",
    json=payload
)

print("Status:", response.status_code)
print("Response:", response.text)
