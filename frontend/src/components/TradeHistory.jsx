import { useMemo, useState } from "react";

export default function TradeHistory({ trades }) {
  const [searchTerm, setSearchTerm] = useState("");
  const [pairFilter, setPairFilter] = useState("ALL");
  const [sideFilter, setSideFilter] = useState("ALL");
  const [statusFilter, setStatusFilter] = useState("ALL");
  const [sortConfig, setSortConfig] = useState({
    key: "executedAtUtc",
    direction: "desc",
  });

  const pairs = useMemo(
    () => Array.from(new Set(trades.map((trade) => trade.pair))).sort(),
    [trades]
  );

  const statuses = useMemo(
    () => Array.from(new Set(trades.map((trade) => trade.status))).sort(),
    [trades]
  );

  const filteredTrades = useMemo(() => {
    const normalizedSearch = searchTerm.trim().toLowerCase();

    return trades
      .filter((trade) => {
        const matchesPair = pairFilter === "ALL" || trade.pair === pairFilter;
        const matchesSide = sideFilter === "ALL" || trade.side === sideFilter;
        const matchesStatus =
          statusFilter === "ALL" || trade.status === statusFilter;
        const matchesSearch =
          normalizedSearch.length === 0 ||
          [
            trade.tradeId,
            trade.portfolioId,
            trade.quoteId,
            trade.pair,
            trade.side,
            trade.baseCurrency,
            trade.quoteCurrency,
            trade.status,
          ]
            .join(" ")
            .toLowerCase()
            .includes(normalizedSearch);

        return matchesPair && matchesSide && matchesStatus && matchesSearch;
      })
      .sort((left, right) => compareTrades(left, right, sortConfig));
  }, [pairFilter, searchTerm, sideFilter, sortConfig, statusFilter, trades]);

  function formatNumber(value) {
    return Number(value).toLocaleString(undefined, {
      maximumFractionDigits: 5,
    });
  }

  function compareTrades(left, right, sort) {
    const leftValue = getSortValue(left, sort.key);
    const rightValue = getSortValue(right, sort.key);

    let result = 0;

    if (typeof leftValue === "number" && typeof rightValue === "number") {
      result = leftValue - rightValue;
    } else {
      result = String(leftValue).localeCompare(String(rightValue));
    }

    return sort.direction === "asc" ? result : -result;
  }

  function getSortValue(trade, key) {
    if (key === "executedAtUtc") {
      return new Date(trade.executedAtUtc).getTime();
    }

    if (["baseAmount", "price", "quoteAmount", "realizedPnl"].includes(key)) {
      return Number(trade[key]);
    }

    return trade[key] ?? "";
  }

  function handleSort(key) {
    setSortConfig((current) => {
      if (current.key !== key) {
        return {
          key,
          direction: key === "executedAtUtc" ? "desc" : "asc",
        };
      }

      return {
        key,
        direction: current.direction === "asc" ? "desc" : "asc",
      };
    });
  }

  function sortLabel(key, label) {
    if (sortConfig.key !== key) {
      return label;
    }

    return `${label} ${sortConfig.direction === "asc" ? "up" : "down"}`;
  }

  function resetFilters() {
    setSearchTerm("");
    setPairFilter("ALL");
    setSideFilter("ALL");
    setStatusFilter("ALL");
    setSortConfig({
      key: "executedAtUtc",
      direction: "desc",
    });
  }

  function getPnlClass(value) {
    const pnl = Number(value ?? 0);

    if (pnl > 0) {
      return "pnl-positive";
    }

    if (pnl < 0) {
      return "pnl-negative";
    }

    return "pnl-neutral";
  }

  return (
    <section>
      <div className="section-header">
        <div>
          <h2 className="card-title">Trade History</h2>
          <p className="section-meta">
            Showing {filteredTrades.length} of {trades.length} trades
          </p>
        </div>

        <button className="secondary-button compact-button" onClick={resetFilters}>
          Reset
        </button>
      </div>

      <div className="filter-bar">
        <div className="field-group">
          <label className="field-label">Search</label>
          <input
            className="field-input"
            type="search"
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
            placeholder="Trade, quote, pair"
          />
        </div>

        <div className="field-group">
          <label className="field-label">Pair</label>
          <select
            className="field-select"
            value={pairFilter}
            onChange={(event) => setPairFilter(event.target.value)}
          >
            <option value="ALL">All pairs</option>
            {pairs.map((pair) => (
              <option key={pair} value={pair}>
                {pair}
              </option>
            ))}
          </select>
        </div>

        <div className="field-group">
          <label className="field-label">Side</label>
          <select
            className="field-select"
            value={sideFilter}
            onChange={(event) => setSideFilter(event.target.value)}
          >
            <option value="ALL">All sides</option>
            <option value="BUY">Buy</option>
            <option value="SELL">Sell</option>
          </select>
        </div>

        <div className="field-group">
          <label className="field-label">Status</label>
          <select
            className="field-select"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
          >
            <option value="ALL">All statuses</option>
            {statuses.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </div>
      </div>

      {trades.length === 0 ? (
        <p className="empty-state">No trades yet.</p>
      ) : filteredTrades.length === 0 ? (
        <p className="empty-state">No trades match the current filters.</p>
      ) : (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("tradeId")}
                  >
                    {sortLabel("tradeId", "Trade ID")}
                  </button>
                </th>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("pair")}
                  >
                    {sortLabel("pair", "Pair")}
                  </button>
                </th>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("side")}
                  >
                    {sortLabel("side", "Side")}
                  </button>
                </th>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("baseAmount")}
                  >
                    {sortLabel("baseAmount", "Base Amount")}
                  </button>
                </th>
                <th>Base CCY</th>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("price")}
                  >
                    {sortLabel("price", "Price")}
                  </button>
                </th>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("quoteAmount")}
                  >
                    {sortLabel("quoteAmount", "Quote Amount")}
                  </button>
                </th>
                <th>Quote CCY</th>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("realizedPnl")}
                  >
                    {sortLabel("realizedPnl", "Realized P&L")}
                  </button>
                </th>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("status")}
                  >
                    {sortLabel("status", "Status")}
                  </button>
                </th>
                <th>
                  <button
                    className="sort-button"
                    onClick={() => handleSort("executedAtUtc")}
                  >
                    {sortLabel("executedAtUtc", "Executed UTC")}
                  </button>
                </th>
              </tr>
            </thead>

            <tbody>
              {filteredTrades.map((trade) => (
                <tr key={trade.tradeId}>
                  <td>{trade.tradeId}</td>
                  <td>{trade.pair}</td>
                  <td>{trade.side}</td>
                  <td>{formatNumber(trade.baseAmount)}</td>
                  <td>{trade.baseCurrency}</td>
                  <td>{formatNumber(trade.price)}</td>
                  <td>{formatNumber(trade.quoteAmount)}</td>
                  <td>{trade.quoteCurrency}</td>
                  <td className={getPnlClass(trade.realizedPnl)}>
                    {formatNumber(trade.realizedPnl)}
                  </td>
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
