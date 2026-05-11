export default function PositionsTable({ positions }) {
  function formatNumber(value) {
    return Number(value).toLocaleString(undefined, {
      maximumFractionDigits: 5,
    });
  }

  function getPnlClass(value) {
    const pnl = Number(value);

    if (pnl > 0) {
      return "pnl-positive";
    }

    if (pnl < 0) {
      return "pnl-negative";
    }

    return "pnl-neutral";
  }

  function formatPnl(value, currency) {
    return `${Number(value).toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })} ${currency}`;
  }

  return (
    <section>
      <h2 className="card-title">Positions</h2>

      {positions.length === 0 ? (
        <p className="empty-state">No positions yet.</p>
      ) : (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Pair</th>
                <th>Net Base Amount</th>
                <th>Base CCY</th>
                <th>Average Price</th>
                <th>Current Price</th>
                <th>Unrealized P&amp;L</th>
                <th>Realized P&amp;L</th>
                <th>P&amp;L CCY</th>
                <th>Updated UTC</th>
              </tr>
            </thead>

            <tbody>
              {positions.map((position) => (
                <tr key={position.pair}>
                  <td>{position.pair}</td>
                  <td>{formatNumber(position.netBaseAmount)}</td>
                  <td>{position.baseCurrency}</td>
                  <td>{formatNumber(position.averagePrice)}</td>
                  <td>{formatNumber(position.currentPrice)}</td>
                  <td className={getPnlClass(position.unrealizedPnl)}>
                    {formatPnl(position.unrealizedPnl, position.pnlCurrency)}
                  </td>
                  <td className={getPnlClass(position.realizedPnl)}>
                    {formatPnl(position.realizedPnl, position.pnlCurrency)}
                  </td>
                  <td>{position.pnlCurrency}</td>
                  <td>{position.updatedAtUtc}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
