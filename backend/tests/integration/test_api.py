from datetime import datetime, timedelta

import pytest

FUTURE_DATE = (datetime.now().date() + timedelta(days=2)).isoformat()
PAST_DATE = (datetime.now().date() - timedelta(days=1)).isoformat()


def _create(client, user_id="maria", field_id=1, date=None, start="10:00", end="12:00"):
    return client.post("/api/reservations", json={
        "user_id": user_id,
        "field_id": field_id,
        "date": date or FUTURE_DATE,
        "start_time": start,
        "end_time": end,
    })


# --- Fields ---

def test_get_availability_returns_3_fields(client):
    res = client.get(f"/api/fields/availability?date={FUTURE_DATE}")
    assert res.status_code == 200
    data = res.json()
    assert len(data) == 3
    names = {f["field_name"] for f in data}
    assert names == {"Cancha A", "Cancha B", "Cancha C"}


def test_get_availability_past_date_returns_400(client):
    res = client.get(f"/api/fields/availability?date={PAST_DATE}")
    assert res.status_code == 400
    assert res.json()["error_type"] == "INVALID_DATE"


# --- Create reservation ---

def test_create_valid_reservation(client):
    res = _create(client)
    assert res.status_code == 201
    body = res.json()
    assert body["status"] == "active"
    assert body["field_name"] == "Cancha A"
    assert "reservation_id" in body


def test_create_field_not_found(client):
    res = _create(client, field_id=999)
    assert res.status_code == 422
    assert res.json()["error_type"] == "FIELD_NOT_FOUND"


def test_create_duration_invalid(client):
    res = _create(client, start="10:00", end="10:30")
    assert res.status_code == 422
    assert res.json()["error_type"] == "DURATION_INVALID"


def test_create_invalid_block(client):
    res = _create(client, start="10:15", end="11:15")
    assert res.status_code == 422
    assert res.json()["error_type"] == "INVALID_BLOCK"


def test_create_operating_hours(client):
    res = _create(client, start="05:00", end="06:00")
    assert res.status_code == 422
    assert res.json()["error_type"] == "OPERATING_HOURS"


def test_create_advance_notice(client):
    res = _create(client, date=PAST_DATE, start="10:00", end="12:00")
    assert res.status_code == 422
    assert res.json()["error_type"] == "ADVANCE_NOTICE"


def test_create_overlap(client):
    _create(client, user_id="pedro", field_id=1, start="10:00", end="12:00")
    res = _create(client, user_id="juan", field_id=1, start="11:00", end="13:00")
    assert res.status_code == 422
    assert res.json()["error_type"] == "OVERLAP"


def test_create_active_limit(client):
    _create(client, user_id="maria", field_id=1, start="10:00", end="11:00")
    _create(client, user_id="maria", field_id=2, start="10:00", end="11:00")
    res = _create(client, user_id="maria", field_id=3, start="10:00", end="11:00")
    assert res.status_code == 422
    assert res.json()["error_type"] == "ACTIVE_LIMIT"


# --- List reservations ---

def test_list_reservations_empty(client):
    res = client.get("/api/reservations?user_id=newuser")
    assert res.status_code == 200
    assert res.json() == []


def test_list_reservations_returns_active(client):
    _create(client, user_id="maria")
    res = client.get("/api/reservations?user_id=maria")
    assert res.status_code == 200
    assert len(res.json()) == 1


# --- Cancel ---

def _cancel(client, reservation_id, user_id):
    return client.request("DELETE", f"/api/reservations/{reservation_id}", json={"user_id": user_id})


def test_cancel_reservation(client):
    r = _create(client, user_id="maria")
    res_id = r.json()["reservation_id"]
    res = _cancel(client, res_id, "maria")
    assert res.status_code == 200
    body = res.json()
    assert body["status"] == "cancelled"
    assert "no_show" in body


def test_cancel_not_found(client):
    res = _cancel(client, "nonexistent-id", "maria")
    assert res.status_code == 404
    assert res.json()["error_type"] == "NOT_FOUND"


def test_cancel_not_authorized(client):
    r = _create(client, user_id="maria")
    res_id = r.json()["reservation_id"]
    res = _cancel(client, res_id, "pedro")
    assert res.status_code == 403
    assert res.json()["error_type"] == "NOT_AUTHORIZED"


def test_cancel_already_cancelled(client):
    r = _create(client, user_id="maria")
    res_id = r.json()["reservation_id"]
    _cancel(client, res_id, "maria")
    res = _cancel(client, res_id, "maria")
    assert res.status_code == 400
    assert res.json()["error_type"] == "ALREADY_CANCELLED"
