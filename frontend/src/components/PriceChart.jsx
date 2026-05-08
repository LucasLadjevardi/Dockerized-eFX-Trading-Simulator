import { useEffect, useState } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
} from "recharts";

import { getPriceHistory } from "../api/efxApi.js";

export default function PriceChart({ livePrices }) {
  const [selectedPair, setSelectedPair] = useState("EURUSD");
  const [history, setHistory] = useState([]);

  useEffect(() => {
    async function loadHistory() {
      const data = await getPriceHistory(selectedPair);
      setHistory(formatHistory(data));
    }

    loadHistory();
  }, [selectedPair]);

  useEffect(() => {
    const latestPrice = livePrices[selectedPair];

    if (!latestPrice) {
      return;
    }

    setHistory((currentHistory) => {
      const nextPoint = formatPoint(latestPrice);
      const updated = [...currentHistory, nextPoint];

      return updated.slice(-120);
    });
  }, [livePrices, selectedPair]);

  function formatHistory(items) {
    return items.map(formatPoint);
  }

  function formatPoint(price) {
    return {
      time: new Date(price.timestampUtc).toLocaleTimeString(),
      mid: Number(price.mid),
      bid: Number(price.bid),
      ask: Number(price.ask),
    };
  }

  return (
    <section>
      <div className="chart-header">
        <h2 className="card-title">Price Chart</h2>

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

      <div className="chart-wrap">
        <ResponsiveContainer width="100%" height={320}>
          <LineChart data={history}>
            <CartesianGrid strokeDasharray="3 3" stroke="#2a3547" />
            <XAxis dataKey="time" stroke="#94a3b8" />
            <YAxis
              stroke="#94a3b8"
              domain={["auto", "auto"]}
              tickFormatter={(value) => Number(value).toFixed(5)}
            />
            <Tooltip
              contentStyle={{
                background: "#111827",
                border: "1px solid #2a3547",
                borderRadius: "10px",
                color: "#e5e7eb",
              }}
              formatter={(value) => Number(value).toFixed(5)}
            />
            <Line
              type="monotone"
              dataKey="mid"
              stroke="#3b82f6"
              strokeWidth={2}
              dot={false}
              isAnimationActive={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </section>
  );
}