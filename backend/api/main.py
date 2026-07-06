from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from api.routes import fields, reservations
from infrastructure.database import SessionLocal, create_tables
from infrastructure.seed import seed_fields


@asynccontextmanager
async def lifespan(app: FastAPI):
    create_tables()
    db = SessionLocal()
    try:
        seed_fields(db)
    finally:
        db.close()
    yield


app = FastAPI(title="Canchas Sintéticas API", lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(fields.router, prefix="/api")
app.include_router(reservations.router, prefix="/api")
