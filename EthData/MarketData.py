import argparse
import json
import math
import sys
from typing import Any

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
        21600,
    }

    def get_ohlcv(
        self,
        pair: str,
        interval: int = 60,
        include_current_candle: bool = False,
    ) -> pd.DataFrame:
        normalized_pair = self._validate_pair(pair)
        normalized_interval = self._validate_interval(interval)

        response = requests.get(
            self.BASE_URL,
            params={
                "pair": normalized_pair,
                "interval": normalized_interval,
            },
            timeout=30,
            headers={
                "User-Agent": "EthStatistics/1.0",
                "Accept": "application/json",
            },
        )

        response.raise_for_status()

        payload = response.json()

        if not isinstance(payload, dict):
            raise RuntimeError(
                "Kraken returned an unexpected response format."
            )

        errors = payload.get("error", [])

        if errors:
            error_message = ", ".join(
                str(error)
                for error in errors
            )

            raise RuntimeError(
                f"Kraken API error: {error_message}"
            )

        result = payload.get("result")

        if not isinstance(result, dict):
            raise RuntimeError(
                "Kraken returned no market-data result."
            )

        pair_keys = [
            key
            for key in result.keys()
            if key != "last"
        ]

        if not pair_keys:
            raise RuntimeError(
                "Kraken returned no OHLC series."
            )

        result_pair_key = pair_keys[0]
        raw_candles = result.get(result_pair_key)

        if not isinstance(raw_candles, list):
            raise RuntimeError(
                "Kraken returned OHLC data in an unexpected format."
            )

        if not raw_candles:
            raise RuntimeError(
                "Kraken returned an empty OHLC series."
            )

        dataframe = pd.DataFrame(
            raw_candles,
            columns=[
                "timestamp",
                "open",
                "high",
                "low",
                "close",
                "vwap",
                "volume",
                "trade_count",
            ],
        )

        if (
            not include_current_candle
            and not dataframe.empty
        ):
            dataframe = dataframe.iloc[:-1].copy()

        if dataframe.empty:
            raise RuntimeError(
                "No completed candles were returned by Kraken."
            )

        dataframe["date"] = pd.to_datetime(
            dataframe["timestamp"],
            unit="s",
            utc=True,
            errors="raise",
        )

        decimal_columns = [
            "open",
            "high",
            "low",
            "close",
            "vwap",
            "volume",
        ]

        for column in decimal_columns:
            dataframe[column] = pd.to_numeric(
                dataframe[column],
                errors="raise",
            ).astype(float)

        dataframe["trade_count"] = pd.to_numeric(
            dataframe["trade_count"],
            errors="raise",
        ).astype("int64")

        dataframe = dataframe[
            [
                "date",
                "open",
                "high",
                "low",
                "close",
                "vwap",
                "volume",
                "trade_count",
            ]
        ]

        dataframe = dataframe.sort_values(
            "date"
        ).reset_index(
            drop=True
        )

        return dataframe

    @staticmethod
    def _validate_pair(pair: str) -> str:
        if pair is None:
            raise ValueError(
                "The trading pair is required."
            )

        normalized_pair = str(pair).strip().upper()

        if not normalized_pair:
            raise ValueError(
                "The trading pair is required."
            )

        return normalized_pair

    def _validate_interval(
        self,
        interval: int,
    ) -> int:
        try:
            normalized_interval = int(interval)
        except (TypeError, ValueError) as error:
            raise ValueError(
                "The interval must be an integer number of minutes."
            ) from error

        if normalized_interval not in self.VALID_INTERVALS:
            valid_values = ", ".join(
                str(value)
                for value in sorted(
                    self.VALID_INTERVALS
                )
            )

            raise ValueError(
                "Unsupported Kraken interval: "
                f"{normalized_interval}. "
                f"Valid intervals: {valid_values}."
            )

        return normalized_interval


def safe_json_number(
    value: Any,
) -> float:
    number = float(value)

    if not math.isfinite(number):
        raise ValueError(
            "A candle contains a non-finite numeric value."
        )

    return number


def dataframe_to_response(
    dataframe: pd.DataFrame,
    pair: str,
    interval: int,
) -> dict[str, Any]:
    candles: list[dict[str, Any]] = []

    for row in dataframe.itertuples(
        index=False
    ):
        candles.append(
            {
                "date": row.date.isoformat(),
                "open": safe_json_number(
                    row.open
                ),
                "high": safe_json_number(
                    row.high
                ),
                "low": safe_json_number(
                    row.low
                ),
                "close": safe_json_number(
                    row.close
                ),
                "vwap": safe_json_number(
                    row.vwap
                ),
                "volume": safe_json_number(
                    row.volume
                ),
                "tradeCount": int(
                    row.trade_count
                ),
            }
        )

    return {
        "success": True,
        "error": None,
        "pair": pair.strip().upper(),
        "interval": int(interval),
        "candles": candles,
    }


def create_error_response(
    error: Exception,
    pair: str,
    interval: int,
) -> dict[str, Any]:
    return {
        "success": False,
        "error": str(error),
        "pair": str(pair).strip().upper(),
        "interval": int(interval),
        "candles": [],
    }


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Retrieve completed OHLC candles "
            "from the Kraken public REST API."
        )
    )

    parser.add_argument(
        "--pair",
        type=str,
        default="ETHUSD",
        help=(
            "Kraken trading pair. "
            "Default: ETHUSD."
        ),
    )

    parser.add_argument(
        "--interval",
        type=int,
        default=60,
        choices=sorted(
            KrakenMarketData.VALID_INTERVALS
        ),
        help=(
            "Candle interval in minutes. "
            "Default: 60."
        ),
    )

    parser.add_argument(
        "--include-current-candle",
        action="store_true",
        help=(
            "Include Kraken's current "
            "incomplete candle."
        ),
    )

    return parser.parse_args()


def main() -> int:
    args = parse_arguments()

    try:
        service = KrakenMarketData()

        dataframe = service.get_ohlcv(
            pair=args.pair,
            interval=args.interval,
            include_current_candle=(
                args.include_current_candle
            ),
        )

        response = dataframe_to_response(
            dataframe=dataframe,
            pair=args.pair,
            interval=args.interval,
        )

        print(
            json.dumps(
                response,
                separators=(",", ":"),
                ensure_ascii=False,
                allow_nan=False,
            )
        )

        return 0

    except Exception as error:
        response = create_error_response(
            error=error,
            pair=args.pair,
            interval=args.interval,
        )

        print(
            json.dumps(
                response,
                separators=(",", ":"),
                ensure_ascii=False,
                allow_nan=False,
            )
        )

        return 1


if __name__ == "__main__":
    sys.exit(main())