export default function ExposureSummaryCards({ positions }) {
  const summary = buildExposureSummary(positions);

  return (
    <section className="exposure-grid" aria-label="Exposure summary">
      <article className="metric-card">
        <span className="metric-label">Open Positions</span>
        <strong className="metric-value">{positions.length}</strong>
        <span className="metric-meta">
          {summary.longCount} long / {summary.shortCount} short
        </span>
      </article>

      <article className="metric-card">
        <span className="metric-label">Gross Notional</span>
        <strong className="metric-value">
          {formatCompact(summary.grossNotional)}
        </strong>
        <span className="metric-meta">quote currency equivalent</span>
      </article>

      <article className="metric-card">
        <span className="metric-label">Unrealized P&amp;L</span>
        <strong
          className={`metric-value ${getToneClass(
            summary.primaryPnl?.amount ?? 0
          )}`}
        >
          {summary.primaryPnl
            ? `${formatMoney(summary.primaryPnl.amount)} ${
                summary.primaryPnl.currency
              }`
            : "0.00"}
        </strong>
        <span className="metric-meta">
          {summary.pnlByCurrency.length > 1
            ? `${summary.pnlByCurrency.length} P&L currencies`
            : "current mark"}
        </span>
      </article>

      <article className="metric-card">
        <span className="metric-label">Largest Pair Exposure</span>
        <strong className="metric-value">
          {summary.largestPair?.pair ?? "-"}
        </strong>
        <span className="metric-meta">
          {summary.largestPair
            ? formatCompact(summary.largestPair.amount)
            : "no exposure"}
        </span>
      </article>

      <article className="metric-card wide">
        <div className="metric-card-header">
          <span className="metric-label">Currency Exposure</span>
          <span className="metric-meta">{summary.currencyExposure.length}</span>
        </div>

        {summary.currencyExposure.length === 0 ? (
          <span className="metric-meta">no currency exposure</span>
        ) : (
          <div className="exposure-list">
            {summary.currencyExposure.map((item) => (
              <div className="exposure-row" key={item.currency}>
                <span className="exposure-currency">{item.currency}</span>
                <span className={getToneClass(item.amount)}>
                  {formatMoney(item.amount)}
                </span>
              </div>
            ))}
          </div>
        )}
      </article>
    </section>
  );
}

function buildExposureSummary(positions) {
  const currencyExposure = new Map();
  let grossNotional = 0;
  let longCount = 0;
  let shortCount = 0;
  let largestPair = null;
  const pnlByCurrency = new Map();

  for (const position of positions) {
    const netBaseAmount = Number(position.netBaseAmount ?? 0);
    const currentPrice = Number(position.currentPrice ?? 0);
    const quoteExposure = -netBaseAmount * currentPrice;
    const pairExposure = Math.abs(netBaseAmount);

    grossNotional += Math.abs(quoteExposure);

    if (netBaseAmount > 0) {
      longCount += 1;
    } else if (netBaseAmount < 0) {
      shortCount += 1;
    }

    if (position.pnlCurrency) {
      pnlByCurrency.set(
        position.pnlCurrency,
        (pnlByCurrency.get(position.pnlCurrency) ?? 0) +
          Number(position.unrealizedPnl ?? 0)
      );
    }

    addCurrencyExposure(currencyExposure, position.baseCurrency, netBaseAmount);
    addCurrencyExposure(currencyExposure, position.quoteCurrency, quoteExposure);

    if (!largestPair || pairExposure > Math.abs(largestPair.amount)) {
      largestPair = {
        pair: position.pair,
        amount: netBaseAmount,
      };
    }
  }

  const pnlBuckets = Array.from(pnlByCurrency.entries())
    .map(([currency, amount]) => ({ currency, amount }))
    .sort((left, right) => Math.abs(right.amount) - Math.abs(left.amount));

  return {
    currencyExposure: Array.from(currencyExposure.entries())
      .map(([currency, amount]) => ({ currency, amount }))
      .filter((item) => item.amount !== 0)
      .sort((left, right) => Math.abs(right.amount) - Math.abs(left.amount))
      .slice(0, 5),
    grossNotional,
    largestPair,
    longCount,
    pnlByCurrency: pnlBuckets,
    primaryPnl: pnlBuckets[0] ?? null,
    shortCount,
  };
}

function addCurrencyExposure(exposures, currency, amount) {
  if (!currency) {
    return;
  }

  exposures.set(currency, (exposures.get(currency) ?? 0) + amount);
}

function getToneClass(value) {
  if (Number(value) > 0) {
    return "pnl-positive";
  }

  if (Number(value) < 0) {
    return "pnl-negative";
  }

  return "pnl-neutral";
}

function formatMoney(value) {
  return Number(value).toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

function formatCompact(value) {
  return Number(value).toLocaleString(undefined, {
    notation: "compact",
    maximumFractionDigits: 2,
  });
}
