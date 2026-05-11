import { useEffect, useMemo, useState } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
  Legend,
} from "recharts";

import { getPriceHistory } from "../api/efxApi.js";

export default function PriceChart({ livePrices }) {
  const [selectedPair, setSelectedPair] = useState("EURUSD");
  const [chartMode, setChartMode] = useState("mid");
  const [windowSize, setWindowSize] = useState(120);
  const [history, setHistory] = useState([]);

  const latestPrice = livePrices[selectedPair];

  const visibleHistory = useMemo(
    () => history.slice(-windowSize),
    [history, windowSize]
  );

  const decimalPlaces = getDecimalPlaces(selectedPair, chartMode);

  useEffect(() => {
    async function loadHistory() {
      const data = await getPriceHistory(selectedPair);
      setHistory(formatHistory(data));
    }

    loadHistory();
  }, [selectedPair]);

  useEffect(() => {
    if (!latestPrice) {
      return;
    }

    setHistory((currentHistory) => {
      const nextPoint = formatPoint(latestPrice);
      const updated = [...currentHistory, nextPoint];

      return updated.slice(-120);
    });
  }, [latestPrice]);

  function formatHistory(items) {
    return items.map(formatPoint);
  }

  function formatPoint(price) {
    return {
      time: new Date(price.timestampUtc).toLocaleTimeString(),
      mid: Number(price.mid),
      bid: Number(price.bid),
      ask: Number(price.ask),
      spread: getSpreadValue(price),
    };
  }

  function formatChartValue(value) {
    return Number(value).toFixed(decimalPlaces);
  }

  function getSpreadValue(price) {
    return Number(price.spread ?? Number(price.ask) - Number(price.bid));
  }

  function getDecimalPlaces(pair, mode) {
    if (mode === "spread") {
      return pair.endsWith("JPY") ? 3 : 5;
    }

    return pair.endsWith("JPY") ? 3 : 5;
  }

  function renderLines() {
    if (chartMode === "bidAsk") {
      return (
        <>
          <Line
            type="monotone"
            dataKey="bid"
            name="Bid"
            stroke="#22c55e"
            strokeWidth={2}
            dot={false}
            isAnimationActive={false}
          />
          <Line
            type="monotone"
            dataKey="ask"
            name="Ask"
            stroke="#ef4444"
            strokeWidth={2}
            dot={false}
            isAnimationActive={false}
          />
        </>
      );
    }

    if (chartMode === "spread") {
      return (
        <Line
          type="monotone"
          dataKey="spread"
          name="Spread"
          stroke="#f59e0b"
          strokeWidth={2}
          dot={false}
          isAnimationActive={false}
        />
      );
    }

    return (
      <Line
        type="monotone"
        dataKey="mid"
        name="Mid"
        stroke="#3b82f6"
        strokeWidth={2}
        dot={false}
        isAnimationActive={false}
      />
    );
  }

  return (
    <section>
      <div className="chart-header">
        <div>
          <h2 className="card-title">Price Chart</h2>
          <p className="section-meta">
            {visibleHistory.length} ticks for {selectedPair}
          </p>
        </div>

        <div className="chart-controls">
          <div className="field-group">
            <label className="field-label">Pair</label>
            <select
              className="field-select chart-select"
              value={selectedPair}
              onChange={(event) => setSelectedPair(event.target.value)}
            >
              <option value="EURUSD">EUR/USD</option>
              <option value="GBPUSD">GBP/USD</option>
              <option value="USDJPY">USD/JPY</option>
              <option value="EURGBP">EUR/GBP</option>
            </select>
          </div>

          <div className="field-group">
            <label className="field-label">View</label>
            <select
              className="field-select chart-select"
              value={chartMode}
              onChange={(event) => setChartMode(event.target.value)}
            >
              <option value="mid">Mid</option>
              <option value="bidAsk">Bid / Ask</option>
              <option value="spread">Spread</option>
            </select>
          </div>

          <div className="field-group">
            <label className="field-label">Window</label>
            <select
              className="field-select chart-select"
              value={windowSize}
              onChange={(event) => setWindowSize(Number(event.target.value))}
            >
              <option value={30}>Last 30</option>
              <option value={60}>Last 60</option>
              <option value={120}>Last 120</option>
            </select>
          </div>
        </div>
      </div>

      {latestPrice && (
        <div className="chart-stats">
          <div className="chart-stat">
            <span className="chart-stat-label">Bid</span>
            <span className="chart-stat-value">
              {Number(latestPrice.bid).toFixed(getDecimalPlaces(selectedPair))}
            </span>
          </div>
          <div className="chart-stat">
            <span className="chart-stat-label">Ask</span>
            <span className="chart-stat-value">
              {Number(latestPrice.ask).toFixed(getDecimalPlaces(selectedPair))}
            </span>
          </div>
          <div className="chart-stat">
            <span className="chart-stat-label">Spread</span>
            <span className="chart-stat-value">
              {getSpreadValue(latestPrice).toFixed(
                getDecimalPlaces(selectedPair, "spread")
              )}
            </span>
          </div>
        </div>
      )}

      <div className="chart-wrap">
        <ResponsiveContainer width="100%" height={320}>
          <LineChart data={visibleHistory}>
            <CartesianGrid strokeDasharray="3 3" stroke="#2a3547" />
            <XAxis dataKey="time" stroke="#94a3b8" />
            <YAxis
              stroke="#94a3b8"
              domain={["auto", "auto"]}
              tickFormatter={formatChartValue}
            />
            <Tooltip
              contentStyle={{
                background: "#111827",
                border: "1px solid #2a3547",
                borderRadius: "10px",
                color: "#e5e7eb",
              }}
              formatter={(value, name) => [formatChartValue(value), name]}
            />
            <Legend wrapperStyle={{ color: "#cbd5e1", fontSize: "13px" }} />
            {renderLines()}
          </LineChart>
        </ResponsiveContainer>
      </div>
    </section>
  );
}
