export default function TradeHistory({ trades }) {
  function formatNumber(value) {
    return Number(value).toLocaleString(undefined, {
      maximumFractionDigits: 5,
    });
  }

  return (
    <section>
      <h2 className="card-title">Trade History</h2>

      {trades.length === 0 ? (
        <p className="empty-state">No trades yet.</p>
      ) : (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Trade ID</th>
                <th>Pair</th>
                <th>Side</th>
                <th>Base Amount</th>
                <th>Base CCY</th>
                <th>Price</th>
                <th>Quote Amount</th>
                <th>Quote CCY</th>
                <th>Status</th>
                <th>Executed UTC</th>
              </tr>
            </thead>

            <tbody>
              {trades.map((trade) => (
                <tr key={trade.tradeId}>
                  <td>{trade.tradeId}</td>
                  <td>{trade.pair}</td>
                  <td>{trade.side}</td>
                  <td>{formatNumber(trade.baseAmount)}</td>
                  <td>{trade.baseCurrency}</td>
                  <td>{formatNumber(trade.price)}</td>
                  <td>{formatNumber(trade.quoteAmount)}</td>
                  <td>{trade.quoteCurrency}</td>
                  <td>{trade.status}</td>
                  <td>{trade.executedAtUtc}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}