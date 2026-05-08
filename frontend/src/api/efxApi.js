const API_BASE = "";

export async function requestQuote({ pair, side, amount }) {
  const response = await fetch(`${API_BASE}/api/quotes`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      pair,
      side,
      amount: Number(amount)
    })
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error ?? "Failed to request quote");
  }

  return response.json();
}

export async function executeTrade(quoteId) {
  const response = await fetch(`${API_BASE}/api/trades`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ quoteId })
  });

  return response.json();
}

export async function getPositions() {
  const response = await fetch(`${API_BASE}/api/positions`);
  return response.json();
}

export async function getTrades() {
  const response = await fetch(`${API_BASE}/api/trades`);
  return response.json();
}

export async function getPriceHistory(pair) {
  const response = await fetch(`/api/prices/history/${pair}`);
  return response.json();
}