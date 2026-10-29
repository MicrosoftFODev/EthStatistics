import argparse
import json
import math
import sys
from typing import Any

import pandas as pd
import requests


class KrakenMarketData:
    ASSET_PAIRS_URL = (
        "https://api.kraken.com/0/public/AssetPairs"
    )

    OHLC_URL = (
        "https://api.kraken.com/0/public/OHLC"
    )

    DEFAULT_PAIR = "ETHUSD"
    DEFAULT_INTERVAL = 1440

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

    REQUEST_TIMEOUT = 30

    REQUEST_HEADERS = {
        "User-Agent": "EthStatistics/1.0",
        "Accept": "application/json",
    }

    # ==========================================
    # METADATA
    # ==========================================

    def get_metadata(
        self,
    ) -> dict[str, Any]:
        response = requests.get(
            self.ASSET_PAIRS_URL,
            timeout=self.REQUEST_TIMEOUT,
            headers=self.REQUEST_HEADERS,
        )

        response.raise_for_status()

        payload = response.json()

        self._validate_kraken_payload(
            payload
        )

        result = payload.get("result")

        if not isinstance(result, dict):
            raise RuntimeError(
                "Kraken returned no asset-pair metadata."
            )

        markets: list[dict[str, Any]] = []

        for internal_pair, pair_data in result.items():
            if not isinstance(pair_data, dict):
                continue

            status = str(
                pair_data.get(
                    "status",
                    "",
                )
            ).strip().lower()

            if (
                status
                and status != "online"
            ):
                continue

            rest_pair = str(
                pair_data.get(
                    "altname",
                    internal_pair,
                )
            ).strip().upper()

            display_pair = str(
                pair_data.get(
                    "wsname",
                    "",
                )
            ).strip().upper()

            if not rest_pair:
                continue

            base_asset, quote_asset = (
                self._extract_assets(
                    display_pair,
                    pair_data,
                )
            )

            if (
                not base_asset
                or not quote_asset
            ):
                continue

            if not display_pair:
                display_pair = (
                    f"{base_asset}/{quote_asset}"
                )

            markets.append(
                {
                    "pair": rest_pair,
                    "displayName": display_pair,
                    "baseAsset": base_asset,
                    "quoteAsset": quote_asset,
                }
            )

        markets = self._remove_duplicate_markets(
            markets
        )

        markets.sort(
            key=lambda market: (
                market["baseAsset"],
                market["quoteAsset"],
                market["pair"],
            )
        )

        if not markets:
            raise RuntimeError(
                "Kraken returned no usable online markets."
            )

        intervals = self._create_intervals()

        periods = self._create_periods()

        quote_currencies = sorted(
            {
                market["quoteAsset"]
                for market in markets
            }
        )

        return {
            "success": True,
            "error": None,
            "markets": markets,
            "intervals": intervals,
            "periods": periods,
            "quoteCurrencies": quote_currencies,
        }

    # ==========================================
    # OHLCV DATA
    # ==========================================

    def get_ohlcv(
        self,
        pair: str,
        interval: int = DEFAULT_INTERVAL,
        include_current_candle: bool = False,
    ) -> pd.DataFrame:
        normalized_pair = self._validate_pair(
            pair
        )

        normalized_interval = (
            self._validate_interval(
                interval
            )
        )

        response = requests.get(
            self.OHLC_URL,
            params={
                "pair": normalized_pair,
                "interval": normalized_interval,
            },
            timeout=self.REQUEST_TIMEOUT,
            headers=self.REQUEST_HEADERS,
        )

        response.raise_for_status()

        payload = response.json()

        self._validate_kraken_payload(
            payload
        )

        result = payload.get("result")

        if not isinstance(result, dict):
            raise RuntimeError(
                "Kraken returned no OHLC result."
            )

        pair_keys = [
            key
            for key in result
            if key != "last"
        ]

        if not pair_keys:
            raise RuntimeError(
                "Kraken returned no OHLC series."
            )

        response_pair = pair_keys[0]

        raw_candles = result.get(
            response_pair
        )

        if not isinstance(raw_candles, list):
            raise RuntimeError(
                "Kraken returned OHLC data "
                "in an unexpected format."
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

        # Kraken places the current incomplete
        # candle at the end of the response.
        if (
            not include_current_candle
            and not dataframe.empty
        ):
            dataframe = (
                dataframe.iloc[:-1].copy()
            )

        if dataframe.empty:
            raise RuntimeError(
                "Kraken returned no completed candles."
            )

        dataframe["date"] = pd.to_datetime(
            dataframe["timestamp"],
            unit="s",
            utc=True,
            errors="raise",
        )

        numeric_columns = [
            "open",
            "high",
            "low",
            "close",
            "vwap",
            "volume",
        ]

        for column in numeric_columns:
            dataframe[column] = pd.to_numeric(
                dataframe[column],
                errors="raise",
            ).astype(float)

        dataframe["trade_count"] = (
            pd.to_numeric(
                dataframe["trade_count"],
                errors="raise",
            )
            .astype("int64")
        )

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
            by="date",
            ascending=True,
        ).reset_index(
            drop=True
        )

        return dataframe

    # ==========================================
    # METADATA DEFINITIONS
    # ==========================================

    @staticmethod
    def _create_intervals(
    ) -> list[dict[str, Any]]:
        return [
            {
                "name": "1 Minute",
                "minutes": 1,
                "isDefault": False,
            },
            {
                "name": "5 Minutes",
                "minutes": 5,
                "isDefault": False,
            },
            {
                "name": "15 Minutes",
                "minutes": 15,
                "isDefault": False,
            },
            {
                "name": "30 Minutes",
                "minutes": 30,
                "isDefault": False,
            },
            {
                "name": "1 Hour",
                "minutes": 60,
                "isDefault": False,
            },
            {
                "name": "4 Hours",
                "minutes": 240,
                "isDefault": False,
            },
            {
                "name": "1 Day",
                "minutes": 1440,
                "isDefault": True,
            },
            {
                "name": "1 Week",
                "minutes": 10080,
                "isDefault": False,
            },
            {
                "name": "15 Days",
                "minutes": 21600,
                "isDefault": False,
            },
        ]

    @staticmethod
    def _create_periods(
    ) -> list[dict[str, Any]]:
        return [
            {
                "name": "30 Days",
                "days": 30,
                "isDefault": False,
            },
            {
                "name": "90 Days",
                "days": 90,
                "isDefault": False,
            },
            {
                "name": "180 Days",
                "days": 180,
                "isDefault": False,
            },
            {
                "name": "1 Year",
                "days": 365,
                "isDefault": True,
            },
            {
                "name": "Maximum",
                "days": 720,
                "isDefault": False,
            },
        ]

    # ==========================================
    # VALIDATION
    # ==========================================

    @staticmethod
    def _validate_kraken_payload(
        payload: Any,
    ) -> None:
        if not isinstance(payload, dict):
            raise RuntimeError(
                "Kraken returned an unexpected "
                "response format."
            )

        errors = payload.get(
            "error",
            [],
        )

        if errors:
            error_message = ", ".join(
                str(error)
                for error in errors
            )

            raise RuntimeError(
                "Kraken API error: "
                f"{error_message}"
            )

    @staticmethod
    def _validate_pair(
        pair: str,
    ) -> str:
        if pair is None:
            raise ValueError(
                "The trading pair is required."
            )

        normalized_pair = (
            str(pair).strip().upper()
        )

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
            normalized_interval = int(
                interval
            )
        except (TypeError, ValueError) as error:
            raise ValueError(
                "The interval must be an integer."
            ) from error

        if (
            normalized_interval
            not in self.VALID_INTERVALS
        ):
            valid_intervals = ", ".join(
                str(value)
                for value in sorted(
                    self.VALID_INTERVALS
                )
            )

            raise ValueError(
                "Unsupported Kraken interval: "
                f"{normalized_interval}. "
                "Valid values: "
                f"{valid_intervals}."
            )

        return normalized_interval

    # ==========================================
    # MARKET NORMALIZATION
    # ==========================================

    @staticmethod
    def _extract_assets(
        display_pair: str,
        pair_data: dict[str, Any],
    ) -> tuple[str, str]:
        if "/" in display_pair:
            parts = display_pair.split(
                "/",
                maxsplit=1,
            )

            return (
                parts[0].strip().upper(),
                parts[1].strip().upper(),
            )

        base_asset = (
            KrakenMarketData._normalize_asset(
                str(
                    pair_data.get(
                        "base",
                        "",
                    )
                )
            )
        )

        quote_asset = (
            KrakenMarketData._normalize_asset(
                str(
                    pair_data.get(
                        "quote",
                        "",
                    )
                )
            )
        )

        return (
            base_asset,
            quote_asset,
        )

    @staticmethod
    def _normalize_asset(
        asset: str,
    ) -> str:
        normalized = asset.strip().upper()

        aliases = {
            "XXBT": "XBT",
            "XBT": "XBT",
            "XETH": "ETH",
            "ETH": "ETH",
            "XLTC": "LTC",
            "LTC": "LTC",
            "XXRP": "XRP",
            "XRP": "XRP",
            "XXDG": "XDG",
            "XDG": "XDG",
            "ZUSD": "USD",
            "USD": "USD",
            "ZEUR": "EUR",
            "EUR": "EUR",
            "ZGBP": "GBP",
            "GBP": "GBP",
            "ZJPY": "JPY",
            "JPY": "JPY",
            "ZCAD": "CAD",
            "CAD": "CAD",
            "ZAUD": "AUD",
            "AUD": "AUD",
            "ZCHF": "CHF",
            "CHF": "CHF",
        }

        return aliases.get(
            normalized,
            normalized,
        )

    @staticmethod
    def _remove_duplicate_markets(
        markets: list[dict[str, Any]],
    ) -> list[dict[str, Any]]:
        unique_markets: dict[
            str,
            dict[str, Any]
        ] = {}

        for market in markets:
            pair = str(
                market.get(
                    "pair",
                    "",
                )
            ).strip().upper()

            if not pair:
                continue

            if pair not in unique_markets:
                unique_markets[pair] = market

        return list(
            unique_markets.values()
        )


# ==========================================
# JSON CONVERSION
# ==========================================

def safe_json_number(
    value: Any,
) -> float:
    number = float(value)

    if not math.isfinite(number):
        raise ValueError(
            "A candle contains a non-finite "
            "numeric value."
        )

    return number


def dataframe_to_market_response(
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


def create_market_error_response(
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


def create_metadata_error_response(
    error: Exception,
) -> dict[str, Any]:
    return {
        "success": False,
        "error": str(error),
        "markets": [],
        "intervals": [],
        "periods": [],
        "quoteCurrencies": [],
    }


def print_json(
    response: dict[str, Any],
) -> None:
    serialized_response = json.dumps(
        response,
        separators=(",", ":"),
        ensure_ascii=False,
        allow_nan=False,
    )

    print(
        serialized_response,
        flush=True,
    )


# ==========================================
# COMMAND-LINE ARGUMENTS
# ==========================================

def parse_arguments(
) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Kraken metadata and daily OHLC "
            "service for EthStatistics."
        )
    )

    parser.add_argument(
        "--mode",
        choices=[
            "metadata",
            "ohlcv",
        ],
        default="ohlcv",
        help=(
            "Execution mode. "
            "Default: ohlcv."
        ),
    )

    parser.add_argument(
        "--pair",
        type=str,
        default=(
            KrakenMarketData.DEFAULT_PAIR
        ),
        help=(
            "Kraken REST pair. "
            "Default: ETHUSD."
        ),
    )

    parser.add_argument(
        "--interval",
        type=int,
        default=(
            KrakenMarketData.DEFAULT_INTERVAL
        ),
        choices=sorted(
            KrakenMarketData.VALID_INTERVALS
        ),
        help=(
            "Candle interval in minutes. "
            "Default: 1440 for daily candles."
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


# ==========================================
# APPLICATION ENTRY POINT
# ==========================================

def main() -> int:
    args = parse_arguments()

    service = KrakenMarketData()

    try:
        if args.mode == "metadata":
            response = (
                service.get_metadata()
            )

        else:
            dataframe = service.get_ohlcv(
                pair=args.pair,
                interval=args.interval,
                include_current_candle=(
                    args.include_current_candle
                ),
            )

            response = (
                dataframe_to_market_response(
                    dataframe=dataframe,
                    pair=args.pair,
                    interval=args.interval,
                )
            )

        print_json(
            response
        )

        return 0

    except Exception as error:
        if args.mode == "metadata":
            response = (
                create_metadata_error_response(
                    error
                )
            )
        else:
            response = (
                create_market_error_response(
                    error=error,
                    pair=args.pair,
                    interval=args.interval,
                )
            )

        print_json(
            response
        )

        return 1


if __name__ == "__main__":
    sys.exit(
        main()
    )