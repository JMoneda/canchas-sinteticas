# Contract: Fields API

Base path: `/api/fields`

---

## GET /api/fields/availability

Returns all fields with their available 30-minute time slots for the requested date.
Available slots exclude time ranges already occupied by active reservations and
time ranges that cannot be booked due to the 1-hour advance notice rule.

### Request

| Parameter | Location | Type   | Required | Description |
|-----------|----------|--------|----------|-------------|
| `date`    | query    | string | Yes      | Target date in `YYYY-MM-DD` format |

### Responses

**200 OK** — Availability returned successfully.

```json
[
  {
    "field_id": 1,
    "field_name": "Cancha A",
    "available_slots": [
      { "start_time": "06:00", "end_time": "06:30" },
      { "start_time": "06:30", "end_time": "07:00" },
      { "start_time": "07:00", "end_time": "07:30" }
    ]
  },
  {
    "field_id": 2,
    "field_name": "Cancha B",
    "available_slots": []
  }
]
```

- `available_slots` is an array of consecutive 30-minute blocks that are free.
- An empty `available_slots` array means the field is fully booked (or all remaining
  slots fall within the 1-hour advance notice window).

**400 Bad Request** — Date is in the past or malformed.

```json
{
  "error_type": "INVALID_DATE",
  "message": "The requested date is in the past. Please choose today or a future date."
}
```

### Notes

- Time strings use 24-hour format: `"HH:MM"` (e.g., `"06:00"`, `"23:00"`).
- The last possible slot starts at `22:30` (ends at `23:00`).
- Slots are returned in ascending order.
- Slots already started or within the 1-hour advance notice window are excluded.
