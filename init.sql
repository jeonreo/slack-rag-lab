CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS knowledge_cards (
  id SERIAL PRIMARY KEY,
  problem TEXT NOT NULL,
  solution TEXT NOT NULL,
  source_url TEXT,
  embedding VECTOR(1536)
);