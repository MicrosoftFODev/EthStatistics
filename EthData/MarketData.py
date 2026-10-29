import argparse
import json
import sys

import pandas as pd
import requests


class KrakenMarketData:
    BASE_URL = "https://api.kraken.com/0/public/OHLC"

    VALID_INTERVALS = {
        1,
        5,
        15,
        30,
        60,
        240,
        1440,
        10080,
        21600
    }

    def get_ohlcv(
        self,
        pair: str,
        interval: int = 1440,
        include_current_candle: bool = False
    ) -> pd.DataFrame:

        if not pair or not pair.strip():
            raise ValueError("The trading pair is required.")

        if interval not in self.VALID_INTERVALS:
            raise ValueError(
                f"Unsupported Kraken interval: {interval}"
            )

        response = requests.get(
            self.BASE_URL,
            params={
                "pair": pair.strip().upper(),
                "interval": interval
            },
            timeout=30,
            headers={
                "User-Agent": "CryptoStatistics/1.0",
                "Accept": "application/json"
            }
        )

        response.raise_for_status()

        payload = response.json()
        errors = payload.get("error", [])

        if errors:
            raise RuntimeError(
                "Kraken API error: " + ", ".join(errors)
            )

        result = payload.get("result")

        if not result:
            raise RuntimeError(
                "Kraken returned no market data."
            )

        pair_keys = [
            key
            for key in result
            if key != "last"
        ]

        if not pair_keys:
            raise RuntimeError(
                "Kraken returned no OHLC data."
            )

        candles = result[pair_keys[0]]

        df = pd.DataFrame(
            candles,
            columns=[
                "timestamp",
                "open",
                "high",
                "low",
                "close",
                "vwap",
                "volume",
                "trade_count"
            ]
        )

        # Kraken includes the current incomplete candle last.
        if not include_current_candle and not df.empty:
            df = df.iloc[:-1].copy()

        df["date"] = pd.to_datetime(
            df["timestamp"],
            unit="s",
            utc=True
        )

        numeric_columns = [
            "open",
            "high",
            "low",
            "close",
            "vwap",
            "volume"
        ]

        df[numeric_columns] = df[numeric_columns].apply(
            pd.to_numeric,
            errors="raise"
        )

        df["trade_count"] = pd.to_numeric(
            df["trade_count"],
            errors="raise"
        ).astype("int64")

        return (
            df[
                [
                    "date",
                    "open",
                    "high",
                    "low",
                    "close",
                    "vwap",
                    "volume",
                    "trade_count"
                ]
            ]
            .sort_values("date")
            .reset_index(drop=True)
        )


def dataframe_to_response(
    df: pd.DataFrame,
    pair: str,
    interval: int
) -> dict:

    candles = []

    for row in df.itertuples(index=False):
        candles.append(
            {
                "date": row.date.isoformat(),
                "open": row.open,
                "high": row.high,
                "low": row.low,
                "close": row.close,
                "vwap": row.vwap,
                "volume": row.volume,
                "tradeCount": row.trade_count
            }
        )

    return {
        "success": True,
        "error": None,
        "pair": pair.upper(),
        "interval": interval,
        "candles": candles
    }


def main() -> int:
    parser = argparse.ArgumentParser()

    parser.add_argument(
        "--pair",
        required=True
    )

    parser.add_argument(
        "--interval",
        required=True,
        type=int
    )

    args = parser.parse_args()

    try:
        service = KrakenMarketData()

        data = service.get_ohlcv(
            pair=args.pair,
            interval=args.interval
        )

        response = dataframe_to_response(
            data,
            args.pair,
            args.interval
        )

        print(
            json.dumps(
                response,
                separators=(",", ":")
            )
        )

        return 0

    except Exception as error:
        response = {
            "success": False,
            "error": str(error),
            "pair": args.pair.upper(),
            "interval": args.interval,
            "candles": []
        }

        print(
            json.dumps(
                response,
                separators=(",", ":")
            )
        )

        return 1


if __name__ == "__main__":
    sys.exit(main())