import { useEffect, useState } from "react";

export default function QuotePanel({ quote, onExecuteQuote }) {
  const [secondsLeft, setSecondsLeft] = useState(null);

  useEffect(() => {
    if (!quote) {
      setSecondsLeft(null);
      return;
    }

    function updateCountdown() {
      const expiry = new Date(quote.expiresAtUtc).getTime();
      const now = Date.now();
      const remaining = Math.max(0, Math.ceil((expiry - now) / 1000));
      setSecondsLeft(remaining);
    }

    updateCountdown();
    const intervalId = setInterval(updateCountdown, 250);

    return () => clearInterval(intervalId);
  }, [quote]);

  if (!quote) {
    return (
      <section>
        <h2 className="card-title">Quote</h2>
        <p className="empty-state">No quote yet. Request a quote to continue.</p>
      </section>
    );
  }

  const isExpired = secondsLeft === 0;

  return (
    <section>
      <h2 className="card-title">Quote</h2>

      <div className="quote-grid">
        <div className="quote-item">
          <div className="quote-label">Quote ID</div>
          <div className="quote-value">{quote.quoteId}</div>
        </div>

        <div className="quote-item">
          <div className="quote-label">Pair</div>
          <div className="quote-value">{quote.pair}</div>
        </div>

        <div className="quote-item">
          <div className="quote-label">Side</div>
          <div className="quote-value">{quote.side}</div>
        </div>

        <div className="quote-item">
          <div className="quote-label">Amount</div>
          <div className="quote-value">{quote.amount}</div>
        </div>

        <div className="quote-item">
          <div className="quote-label">Price</div>
          <div className="quote-value">{quote.price}</div>
        </div>

        <div className="quote-item">
          <div className="quote-label">Expires In</div>
          <div className="quote-value">{secondsLeft ?? "-"}s</div>
        </div>
      </div>

      <button
        className="primary-button"
        disabled={isExpired}
        onClick={() => onExecuteQuote(quote.quoteId)}
      >
        Execute Trade
      </button>

      {isExpired && (
        <p className="expired-text">Quote expired. Request a new quote.</p>
      )}
    </section>
  );
}