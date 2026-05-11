import { useEffect, useMemo, useState } from "react";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

const MAX_POINTS = 90;

export default function PortfolioPnlChart({ positions }) {
  const [history, setHistory] = useState([]);
  const [viewMode, setViewMode] = useState("timeline");
  const [selectedCurrency, setSelectedCurrency] = useState("USD");

  const summary = useMemo(() => buildPnlSummary(positions), [positions]);
  const selectedPnl = summary.pnlByCurrency[selectedCurrency] ?? 0;

  useEffect(() => {
    if (
      summary.currencies.length > 0 &&
      !summary.currencies.includes(selectedCurrency)
    ) {
      setSelectedCurrency(summary.currencies[0]);
    }
  }, [selectedCurrency, summary.currencies]);

  useEffect(() => {
    setHistory((currentHistory) => {
      const point = {
        time: new Date().toLocaleTimeString(),
        ...summary.pnlByCurrency,
      };

      const lastPoint = currentHistory[currentHistory.length - 1];

      if (
        lastPoint &&
        summary.currencies.every(
          (currency) => lastPoint[currency] === point[currency]
        )
      ) {
        return currentHistory;
      }

      return [...currentHistory, point].slice(-MAX_POINTS);
    });
  }, [summary.currencies, summary.pnlByCurrency]);

  const pairData = useMemo(
    () =>
      positions
        .filter((position) => position.pnlCurrency === selectedCurrency)
        .map((position) => ({
          pair: position.pair,
          pnl: Number(position.unrealizedPnl ?? 0),
          currency: position.pnlCurrency,
        }))
        .sort((left, right) => Math.abs(right.pnl) - Math.abs(left.pnl)),
    [positions, selectedCurrency]
  );

  const hasPositions = positions.length > 0;
  const displayHistory =
    history.length > 0
      ? history
      : [
          {
            time: new Date().toLocaleTimeString(),
            totalPnl: 0,
          },
        ];

  return (
    <section>
      <div className="chart-header">
        <div>
          <h2 className="card-title">P&amp;L Chart</h2>
          <p className="section-meta">
            {formatMoney(selectedPnl)} {selectedCurrency}
          </p>
        </div>

        <div className="pnl-chart-controls">
          <div className="field-group">
            <label className="field-label">Currency</label>
            <select
              className="field-select chart-select"
              value={selectedCurrency}
              onChange={(event) => setSelectedCurrency(event.target.value)}
              disabled={summary.currencies.length === 0}
            >
              {summary.currencies.length === 0 ? (
                <option value="USD">USD</option>
              ) : (
                summary.currencies.map((currency) => (
                  <option value={currency} key={currency}>
                    {currency}
                  </option>
                ))
              )}
            </select>
          </div>

          <div className="segmented-control" aria-label="P&L chart view">
            <button
              className={viewMode === "timeline" ? "active" : ""}
              onClick={() => setViewMode("timeline")}
              type="button"
            >
              Timeline
            </button>
            <button
              className={viewMode === "pairs" ? "active" : ""}
              onClick={() => setViewMode("pairs")}
              type="button"
            >
              Pairs
            </button>
          </div>
        </div>
      </div>

      {!hasPositions ? (
        <p className="empty-state">No open P&amp;L yet.</p>
      ) : (
        <div className="chart-wrap">
          {viewMode === "timeline" ? (
            <ResponsiveContainer width="100%" height={320}>
              <AreaChart data={displayHistory}>
                <defs>
                  <linearGradient id="pnlFill" x1="0" y1="0" x2="0" y2="1">
                    <stop
                      offset="5%"
                      stopColor={selectedPnl >= 0 ? "#22c55e" : "#ef4444"}
                      stopOpacity={0.32}
                    />
                    <stop
                      offset="95%"
                      stopColor={selectedPnl >= 0 ? "#22c55e" : "#ef4444"}
                      stopOpacity={0.02}
                    />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#2a3547" />
                <XAxis dataKey="time" stroke="#94a3b8" />
                <YAxis stroke="#94a3b8" tickFormatter={formatCompactMoney} />
                <Tooltip
                  contentStyle={{
                    background: "#111827",
                    border: "1px solid #2a3547",
                    borderRadius: "10px",
                    color: "#e5e7eb",
                  }}
                  formatter={(value) => [
                    `${formatMoney(value)} ${selectedCurrency}`,
                    "P&L",
                  ]}
                />
                <Area
                  type="monotone"
                  dataKey={selectedCurrency}
                  name="P&L"
                  stroke={selectedPnl >= 0 ? "#22c55e" : "#ef4444"}
                  strokeWidth={2}
                  fill="url(#pnlFill)"
                  isAnimationActive={false}
                />
              </AreaChart>
            </ResponsiveContainer>
          ) : (
            <ResponsiveContainer width="100%" height={320}>
              <BarChart data={pairData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#2a3547" />
                <XAxis dataKey="pair" stroke="#94a3b8" />
                <YAxis stroke="#94a3b8" tickFormatter={formatCompactMoney} />
                <Tooltip
                  contentStyle={{
                    background: "#111827",
                    border: "1px solid #2a3547",
                    borderRadius: "10px",
                    color: "#e5e7eb",
                  }}
                  formatter={(value) => [
                    `${formatMoney(value)} ${selectedCurrency}`,
                    "P&L",
                  ]}
                />
                <Bar dataKey="pnl" name="P&L" radius={[4, 4, 0, 0]}>
                  {pairData.map((item) => (
                    <Cell
                      key={item.pair}
                      fill={item.pnl >= 0 ? "#22c55e" : "#ef4444"}
                    />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      )}
    </section>
  );
}

function buildPnlSummary(positions) {
  const pnlByCurrency = {};

  for (const position of positions) {
    const currency = position.pnlCurrency;

    if (!currency) {
      continue;
    }

    pnlByCurrency[currency] =
      (pnlByCurrency[currency] ?? 0) + Number(position.unrealizedPnl ?? 0);
  }

  return {
    currencies: Object.keys(pnlByCurrency).sort(),
    pnlByCurrency,
  };
}

function formatMoney(value) {
  return Number(value).toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

function formatCompactMoney(value) {
  return Number(value).toLocaleString(undefined, {
    notation: "compact",
    maximumFractionDigits: 1,
  });
}
