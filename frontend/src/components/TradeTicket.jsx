import { useState } from "react";

export default function TradeTicket({ onRequestQuote }) {
  const [pair, setPair] = useState("EURUSD");
  const [side, setSide] = useState("BUY");
  const [amount, setAmount] = useState(1000000);

  function handleSubmit(event) {
    event.preventDefault();

    onRequestQuote({
      pair,
      side,
      amount,
    });
  }

  return (
    <section>
      <h2 className="card-title">Trade Ticket</h2>

      <form className="ticket-form" onSubmit={handleSubmit}>
        <div className="field-group">
          <label className="field-label">Pair</label>
          <select
            className="field-select"
            value={pair}
            onChange={(e) => setPair(e.target.value)}
          >
            <option value="EURUSD">EUR/USD</option>
            <option value="GBPUSD">GBP/USD</option>
            <option value="USDJPY">USD/JPY</option>
            <option value="EURGBP">EUR/GBP</option>
          </select>
        </div>

        <div className="field-group">
          <label className="field-label">Side</label>
          <select
            className="field-select"
            value={side}
            onChange={(e) => setSide(e.target.value)}
          >
            <option value="BUY">Buy</option>
            <option value="SELL">Sell</option>
          </select>
        </div>

        <div className="field-group">
          <label className="field-label">Base Amount</label>
          <input
            className="field-input"
            type="number"
            value={amount}
            min="1"
            step="1"
            onChange={(e) => setAmount(e.target.value)}
          />
        </div>

        <button className="primary-button" type="submit">
          Request Quote
        </button>
      </form>
    </section>
  );
}