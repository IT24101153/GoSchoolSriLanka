"""End-to-end smoke test: run the API, then `python3 -I backend/smoke.py [base_url]`."""
import json
import sys
import time
import urllib.error
import urllib.request
import uuid
from datetime import datetime, timedelta, timezone

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5133/api"
COLOMBO = timezone(timedelta(hours=5, minutes=30))
NOW = datetime.now(COLOMBO)
YEAR, MONTH = NOW.year, NOW.month
RUN = uuid.uuid4().hex[:8]


def call(method, path, body=None, token=None, expect=200, raw=None, content_type="application/json"):
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    req = urllib.request.Request(BASE + path, method=method, data=data)
    req.add_header("Content-Type", content_type)
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(req) as r:
            status, text = r.status, r.read().decode()
    except urllib.error.HTTPError as e:
        status, text = e.code, e.read().decode()
    assert status == expect, f"{method} {path} -> {status} (expected {expect}): {text}"
    return json.loads(text) if text else None


# Admin login
admin = call("POST", "/auth/login", {"email": "admin@goschool.lk", "password": "Admin123!"})
A = admin["token"]
assert admin["user"]["role"] == "Admin"

# Driver registers -> Pending, cannot use driver features yet
driver = call("POST", "/auth/register/driver", {
    "fullName": "Kamal Perera", "email": f"driver-{RUN}@test.lk", "password": "secret123",
    "phoneNumber": "0771234567", "registrationNumber": f"WP-{RUN[:4]}", "capacity": 1,
    "photoUrls": ["http://example.com/van.jpg"], "bankName": "BOC", "bankBranch": "Nugegoda", "bankAccountNumber": "123456",
})
D = driver["token"]
assert driver["user"]["status"] == "Pending"
call("GET", "/driver/me", token=D, expect=403)
call("POST", "/auth/register/driver", {"fullName": "x", "email": f"driver-{RUN}@test.lk", "password": "secret123",
     "phoneNumber": "1", "registrationNumber": "x", "capacity": 1}, expect=409)
call("POST", "/auth/login", {"email": f"driver-{RUN}@test.lk", "password": "wrong"}, expect=401)

# Parent registers -> Active immediately
parent = call("POST", "/auth/register/parent", {
    "fullName": "Nimali Silva", "email": f"parent-{RUN}@test.lk", "password": "secret123",
    "phoneNumber": "0719876543", "homeAddress": "Colombo 07", "homeLatitude": 6.9271, "homeLongitude": 79.8612,
})
P = parent["token"]
parent_id = parent["user"]["id"]

# Admin approves the driver
pending = call("GET", "/admin/drivers?status=Pending", token=A)
driver_id = driver["user"]["id"]
assert any(d["id"] == driver_id for d in pending)
call("POST", f"/admin/drivers/{driver_id}/approve", token=A)
call("POST", f"/admin/drivers/{driver_id}/approve", token=A, expect=409)
me = call("GET", "/driver/me", token=D)
assert me["user"]["status"] == "Active" and me["vehicle"]["approvalStatus"] == "Active"

# Driver sets up a route with stops
route = call("POST", "/driver/routes", {"name": "Nugegoda - Colombo 7", "pricePerDay": 300, "pricePerMonth": 6000}, token=D)
stop1 = call("POST", f"/driver/routes/{route['id']}/stops", {
    "sequence": 1, "school": "Royal College", "pickupAddress": "Reid Ave", "latitude": 6.9065, "longitude": 79.8615, "arrivalTime": "07:15",
}, token=D)
call("POST", f"/driver/routes/{route['id']}/stops", {
    "sequence": 2, "school": "Ananda College", "pickupAddress": "Maradana", "latitude": 6.93, "longitude": 79.87, "arrivalTime": "07:30",
}, token=D)
assert len(call("GET", "/driver/routes", token=D)[0]["stops"]) == 2

# Parent adds a student and finds the driver nearby
student = call("POST", "/parent/students", {
    "name": "Sahan", "school": "Royal College", "schoolAddress": "Reid Ave", "schoolLatitude": 6.9065, "schoolLongitude": 79.8615,
    "primaryContactNumber": "0719876543", "emergencyContactNumber": "0112345678",
}, token=P)
cards = call("GET", "/parent/drivers?school=royal", token=P)
card = next(c for c in cards if c["route"]["id"] == route["id"])
assert card["seatsAvailable"] == 1 and card["distanceKm"] < 5
assert not any(c["route"]["id"] == route["id"] for c in call("GET", "/parent/drivers?school=nonexistent", token=P))

# Seat application: apply, duplicate blocked, driver accepts
app = call("POST", "/parent/applications", {"studentId": student["id"], "routeId": route["id"], "routeStopId": stop1["id"]}, token=P)
call("POST", "/parent/applications", {"studentId": student["id"], "routeId": route["id"], "routeStopId": stop1["id"]}, token=P, expect=409)
inbox = call("GET", "/driver/applications?status=Pending", token=D)
assert [a["id"] for a in inbox] == [app["id"]]
call("POST", f"/driver/applications/{app['id']}/accept", token=D)
mine = call("GET", "/parent/applications", token=P)[0]
assert mine["status"] == "Accepted" and mine["driver"]["bank"]["bankName"] == "BOC" and mine["stop"]["arrivalTime"].startswith("07:15")
call("DELETE", f"/driver/stops/{stop1['id']}", token=D, expect=409)
call("DELETE", f"/parent/students/{student['id']}", token=P, expect=409)

# Capacity: vehicle holds 1, a second student cannot be accepted
student2 = call("POST", "/parent/students", {
    "name": "Dilki", "school": "Royal College", "schoolAddress": "Reid Ave", "schoolLatitude": 6.9065, "schoolLongitude": 79.8615,
    "primaryContactNumber": "0719876543", "emergencyContactNumber": "0112345678",
}, token=P)
app2 = call("POST", "/parent/applications", {"studentId": student2["id"], "routeId": route["id"], "routeStopId": stop1["id"]}, token=P)
full = call("POST", f"/driver/applications/{app2['id']}/accept", token=D, expect=409)
assert full["message"] == "Vehicle is full"
call("POST", f"/driver/applications/{app2['id']}/decline", token=D)

# Payments: parent submits bank slip, driver confirms, reminders then find nobody unpaid
call("POST", "/parent/payments", {"applicationId": app["id"], "billingYear": YEAR, "billingMonth": MONTH, "method": "Bank"}, token=P, expect=400)
assert call("POST", "/driver/reminders", {}, token=D)["sent"] == 1
pay = call("POST", "/parent/payments", {"applicationId": app["id"], "billingYear": YEAR, "billingMonth": MONTH, "method": "Bank",
                                         "slipImageUrl": "http://example.com/slip.jpg"}, token=P)
assert pay["status"] == "Pending" and pay["amount"] == 6000
sheet = call("GET", "/driver/payments", token=D)
assert sheet["items"][0]["payment"]["status"] == "Pending"
call("POST", f"/driver/payments/{pay['id']}/confirm", token=D)
call("POST", f"/driver/payments/{pay['id']}/confirm", token=D, expect=409)
assert call("POST", "/driver/reminders", {}, token=D)["sent"] == 0
call("POST", "/parent/payments", {"applicationId": app["id"], "billingYear": YEAR, "billingMonth": MONTH, "method": "Cash"}, token=P, expect=409)
cash = call("POST", "/driver/payments", {"applicationId": app["id"], "billingYear": YEAR, "billingMonth": MONTH + 1 if MONTH < 12 else 1}, token=D)
assert cash["method"] == "Cash" and cash["status"] == "Completed"

# Driver subscriptions: admin bills the month, marks paid, dashboard reflects it
created = call("POST", "/admin/subscriptions/generate", {"year": YEAR, "month": MONTH, "amount": 1500}, token=A)["created"]
assert created >= 1
assert call("POST", "/admin/subscriptions/generate", {"year": YEAR, "month": MONTH, "amount": 1500}, token=A)["created"] == 0
sub = next(s["subscription"] for s in call("GET", f"/admin/subscriptions?year={YEAR}&month={MONTH}", token=A) if s["driver"]["id"] == driver_id)
assert sub["status"] == "Pending"
call("POST", f"/admin/subscriptions/{sub['id']}/mark-paid", token=A)
assert call("GET", "/driver/subscriptions", token=D)[0]["status"] == "Completed"
dash = call("GET", "/admin/dashboard", token=A)
assert dash["activeDrivers"] >= 1 and dash["subscriptions"]["paid"] >= 1

# Admin read-only views and notifications
assert any(s["id"] == student["id"] and s["assignment"]["route"] == route["name"] for s in call("GET", "/admin/students", token=A))
assert any(p["payment"]["id"] == pay["id"] for p in call("GET", f"/admin/payments?year={YEAR}&month={MONTH}", token=A))
detail = call("GET", f"/admin/drivers/{driver_id}", token=A)
assert detail["acceptedSeats"] == 1 and len(detail["routes"][0]["stops"]) == 2
notes = call("GET", "/notifications", token=P)
assert len(notes) >= 3
call("POST", f"/notifications/{notes[0]['id']}/read", token=P, expect=204)

# File upload
boundary = "----smoke"
body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"slip.png\"\r\nContent-Type: image/png\r\n\r\n").encode()
body += b"\x89PNG\r\n\x1a\nfake" + f"\r\n--{boundary}--\r\n".encode()
url = call("POST", "/uploads", raw=body, token=P, content_type=f"multipart/form-data; boundary={boundary}")["url"]
assert url.endswith(".png")
with urllib.request.urlopen(url) as r:
    assert r.status == 200
assert call("PUT", "/auth/vehicle-photos", {"photoUrls": [url]}, token=D)["photoUrls"] == [url]
call("POST", "/uploads", raw=body.replace(b"slip.png", b"evil.exe"), token=P, content_type=f"multipart/form-data; boundary={boundary}", expect=400)

# Access management: suspension kills the session immediately, reactivation restores login
call("POST", f"/admin/users/{parent_id}/suspend", token=A)
call("GET", "/parent/students", token=P, expect=401)
call("POST", "/auth/login", {"email": f"parent-{RUN}@test.lk", "password": "secret123"}, expect=403)
call("POST", f"/admin/users/{parent_id}/reactivate", token=A)
P = call("POST", "/auth/login", {"email": f"parent-{RUN}@test.lk", "password": "secret123"})["token"]
assert len(call("GET", "/parent/students", token=P)) == 2
call("POST", "/auth/logout", token=P, expect=204)
call("GET", "/parent/students", token=P, expect=401)

print(f"SMOKE OK ({RUN})")
