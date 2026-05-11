import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

import PriceBoard from "./components/PriceBoard.jsx";
import TradeTicket from "./components/TradeTicket.jsx";
import QuotePanel from "./components/QuotePanel.jsx";
import PositionsTable from "./components/Positions.jsx";
import TradeHistory from "./components/TradeHistory.jsx";
import PriceChart from "./components/PriceChart.jsx";
import PortfolioPnlChart from "./components/PortfolioPnlChart.jsx";
import ExposureSummaryCards from "./components/ExposureSummaryCards.jsx";

import {
  executeTrade,
  getPositions,
  getTrades,
  requestQuote,
} from "./api/efxApi.js";

export default function App() {
  const [activeView, setActiveView] = useState("dashboard");

  const [prices, setPrices] = useState({});
  const [connectionStatus, setConnectionStatus] = useState("Disconnected");

  const [quote, setQuote] = useState(null);
  const [positions, setPositions] = useState([]);
  const [trades, setTrades] = useState([]);
  const [message, setMessage] = useState("");

  const positionsRef = useRef([]);

  useEffect(() => {
    positionsRef.current = positions;
  }, [positions]);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/prices")
      .withAutomaticReconnect()
      .build();

    connection.on("pricesUpdated", (updatedPrices) => {
      setPrices(updatedPrices);
    });

    connection.on("positionsUpdated", (updatedPositions) => {
      setPositions(updatedPositions);
    });

    connection
      .start()
      .then(() => setConnectionStatus("Connected"))
      .catch((error) => {
        console.error("SignalR connection failed:", error);
        setConnectionStatus("Failed");
      });

    connection.onreconnecting(() => setConnectionStatus("Reconnecting"));
    connection.onreconnected(() => setConnectionStatus("Connected"));
    connection.onclose(() => setConnectionStatus("Disconnected"));

    return () => {
      connection.stop();
    };
  }, []);

  useEffect(() => {
    refreshPositionsAndTrades();
  }, []);

  async function refreshPositionsAndTrades() {
    try {
      const [latestPositions, latestTrades] = await Promise.all([
        getPositions(),
        getTrades(),
      ]);

      setPositions(latestPositions);
      setTrades(latestTrades);
    } catch (error) {
      console.error("Failed to refresh positions/trades:", error);
    }
  }

  async function handleRequestQuote(request) {
    try {
      setMessage("Requesting quote...");
      setQuote(null);

      const newQuote = await requestQuote(request);

      setQuote(newQuote);
      setMessage("Quote received.");
      setActiveView("trading");
    } catch (error) {
      setMessage(error.message);
    }
  }

  async function handleExecuteQuote(quoteId) {
    try {
      setMessage("Executing trade...");

      const result = await executeTrade(quoteId);

      if (result.status === "REJECTED") {
        setMessage(`Trade rejected: ${result.reason}`);
        return;
      }

      setMessage("Trade filled.");
      setQuote(null);

      await refreshPositionsAndTrades();
      setActiveView("positions");
    } catch (error) {
      setMessage(error.message);
    }
  }

  function renderView() {
    if (activeView === "dashboard") {
      return (
        <>
          <ExposureSummaryCards positions={positions} />

          <div className="card">
            <PriceChart livePrices={prices} />
          </div>

          <div className="card">
            <PortfolioPnlChart positions={positions} />
          </div>

          <div className="card">
            <PriceBoard prices={prices} />
          </div>
        </>
      );
    }

    if (activeView === "trading") {
      return (
        <div className="grid-2">
          <div className="card">
            <TradeTicket onRequestQuote={handleRequestQuote} />
            {message && <div className="message">{message}</div>}
          </div>

          <div className="card">
            <QuotePanel quote={quote} onExecuteQuote={handleExecuteQuote} />
          </div>
        </div>
      );
    }

    if (activeView === "positions") {
      return (
        <>
          <ExposureSummaryCards positions={positions} />

          <div className="grid-2">
            <div className="card">
              <PortfolioPnlChart positions={positions} />
            </div>

            <div className="card">
              <PositionsTable positions={positions} />
            </div>
          </div>
        </>
      );
    }

    if (activeView === "trades") {
      return (
        <div className="card">
          <TradeHistory trades={trades} />
        </div>
      );
    }

    return (
      <>
        <ExposureSummaryCards positions={positions} />

        <div className="card">
          <PriceChart livePrices={prices} />
        </div>

        <div className="card">
          <PortfolioPnlChart positions={positions} />
        </div>

        <div className="card">
          <PriceBoard prices={prices} />
        </div>
      </>
    );
  }

  const statusClass =
    connectionStatus === "Connected"
      ? "connected"
      : connectionStatus === "Reconnecting"
      ? "reconnecting"
      : connectionStatus === "Failed"
      ? "failed"
      : "disconnected";

  return (
    <main className="app-shell">
      <header className="app-header">
        <div>
          <h1 className="app-title">eFX Trading Simulator</h1>
          <p className="app-subtitle">
            Live FX prices, quote requests, trade execution, positions and P&amp;L
          </p>
        </div>

        <div className="status-badge">
          <span className={`status-dot ${statusClass}`}></span>
          <span>SignalR: {connectionStatus}</span>
        </div>
      </header>

      <nav className="nav-bar">
        <button
          className={`nav-button ${activeView === "dashboard" ? "active" : ""}`}
          onClick={() => setActiveView("dashboard")}
        >
          Dashboard
        </button>

        <button
          className={`nav-button ${activeView === "trading" ? "active" : ""}`}
          onClick={() => setActiveView("trading")}
        >
          Trading
        </button>

        <button
          className={`nav-button ${activeView === "positions" ? "active" : ""}`}
          onClick={() => setActiveView("positions")}
        >
          Positions
        </button>

        <button
          className={`nav-button ${activeView === "trades" ? "active" : ""}`}
          onClick={() => setActiveView("trades")}
        >
          Trades
        </button>
      </nav>

      {renderView()}
    </main>
  );
}
