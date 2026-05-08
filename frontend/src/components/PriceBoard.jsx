export default function PriceBoard({ prices }) {
  return (
    <section>
      <h2 className="card-title">Live FX Prices</h2>

      <div className="table-wrap">
        <table className="data-table">
          <thead>
            <tr>
              <th>Pair</th>
              <th>Bid</th>
              <th>Ask</th>
              <th>Mid</th>
              <th>Spread</th>
              <th>Timestamp UTC</th>
            </tr>
          </thead>
          <tbody>
            {Object.entries(prices).map(([pair, price]) => (
              <tr key={pair}>
                <td>{pair}</td>
                <td>{price.bid}</td>
                <td>{price.ask}</td>
                <td>{price.mid}</td>
                <td>{price.spread}</td>
                <td>{price.timestampUtc}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}