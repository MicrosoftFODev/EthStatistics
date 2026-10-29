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
    REQUEST_TIMEOUT = 30

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

    REQUEST_HEADERS = {
        "User-Agent": "EthStatistics/1.0",
        "Accept": "application/json",
    }

    ASSET_ALIASES = {
        # Bitcoin
        "XXBT": "BTC",
        "XBT": "BTC",
        "BTC": "BTC",

        # Ethereum
        "XETH": "ETH",
        "ETH": "ETH",

        # Litecoin
        "XLTC": "LTC",
        "LTC": "LTC",

        # XRP
        "XXRP": "XRP",
        "XRP": "XRP",

        # Dogecoin
        "XXDG": "DOGE",
        "XDG": "DOGE",
        "DOGE": "DOGE",

        # Other legacy Kraken symbols
        "XETC": "ETC",
        "ETC": "ETC",
        "XMLN": "MLN",
        "MLN": "MLN",
        "XREP": "REP",
        "REP": "REP",

        # Fiat currencies
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

    def __init__(self) -> None:
        self._session = requests.Session()

        self._session.headers.update(
            self.REQUEST_HEADERS
        )

    def close(self) -> None:
        self._session.close()

    def __enter__(self) -> "KrakenMarketData":
        return self

    def __exit__(
        self,
        exception_type: Any,
        exception_value: Any,
        traceback: Any,
    ) -> None:
        self.close()

    # ==========================================
    # METADATA
    # ==========================================

    def get_metadata(
        self,
    ) -> dict[str, Any]:
        payload = self._get_json(
            self.ASSET_PAIRS_URL
        )

        result = payload.get("result")

        if not isinstance(result, dict):
            raise RuntimeError(
                "Kraken returned no asset-pair metadata."
            )

        markets: list[dict[str, Any]] = []

        for internal_pair, pair_data in result.items():
            market = self._create_market_metadata(
                internal_pair=internal_pair,
                pair_data=pair_data,
            )

            if market is not None:
                markets.append(
                    market
                )

        markets = self._remove_duplicate_markets(
            markets
        )

        markets.sort(
            key=lambda market: (
                market["quoteAsset"],
                market["baseAsset"],
                market["displayName"],
            )
        )

        if not markets:
            raise RuntimeError(
                "Kraken returned no usable online markets."
            )

        quote_currencies = sorted(
            {
                market["quoteAsset"]
                for market in markets
                if market["quoteAsset"]
            }
        )

        return {
            "success": True,
            "error": None,
            "markets": markets,
            "intervals": self._create_intervals(),
            "periods": self._create_periods(),
            "quoteCurrencies": quote_currencies,
        }

    def _create_market_metadata(
        self,
        internal_pair: str,
        pair_data: Any,
    ) -> dict[str, Any] | None:
        if not isinstance(pair_data, dict):
            return None

        status = str(
            pair_data.get(
                "status",
                "",
            )
        ).strip().lower()

        if status and status != "online":
            return None

        rest_pair = str(
            pair_data.get(
                "altname",
                internal_pair,
            )
        ).strip().upper()

        if not rest_pair:
            return None

        web_socket_name = str(
            pair_data.get(
                "wsname",
                "",
            )
        ).strip().upper()

        base_asset, quote_asset = (
            self._extract_assets(
                web_socket_name=web_socket_name,
                pair_data=pair_data,
            )
        )

        if not base_asset or not quote_asset:
            return None

        # Always construct the display name from the
        # normalized assets. This converts XBT/USD
        # into BTC/USD while retaining XBTUSD as the
        # REST pair sent to Kraken.

        display_name = (
            f"{base_asset}/{quote_asset}"
        )

        return {
            "pair": rest_pair,
            "displayName": display_name,
            "baseAsset": base_asset,
            "quoteAsset": quote_asset,
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

        payload = self._get_json(
            self.OHLC_URL,
            params={
                "pair": normalized_pair,
                "interval": normalized_interval,
            },
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

        raw_candles = result.get(
            pair_keys[0]
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

        # Kraken's last OHLC row is the currently
        # incomplete candle. Exclude it by default.

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

        decimal_columns = [
            "open",
            "high",
            "low",
            "close",
            "vwap",
            "volume",
        ]

        dataframe[decimal_columns] = (
            dataframe[decimal_columns]
            .apply(
                pd.to_numeric,
                errors="raise",
            )
            .astype(float)
        )

        dataframe["trade_count"] = (
            pd.to_numeric(
                dataframe["trade_count"],
                errors="raise",
            )
            .astype("int64")
        )

        return (
            dataframe[
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
            .sort_values(
                by="date",
                ascending=True,
            )
            .reset_index(
                drop=True
            )
        )

    # ==========================================
    # HTTP
    # ==========================================

    def _get_json(
        self,
        url: str,
        params: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        try:
            response = self._session.get(
                url,
                params=params,
                timeout=self.REQUEST_TIMEOUT,
            )

            response.raise_for_status()

        except requests.Timeout as error:
            raise RuntimeError(
                "The Kraken request timed out."
            ) from error

        except requests.ConnectionError as error:
            raise RuntimeError(
                "Kraken could not be reached. "
                "Check the internet connection."
            ) from error

        except requests.HTTPError as error:
            status_code = (
                error.response.status_code
                if error.response is not None
                else "unknown"
            )

            raise RuntimeError(
                "Kraken returned HTTP status "
                f"{status_code}."
            ) from error

        except requests.RequestException as error:
            raise RuntimeError(
                f"Kraken request failed: {error}"
            ) from error

        try:
            payload = response.json()

        except requests.JSONDecodeError as error:
            raise RuntimeError(
                "Kraken returned invalid JSON."
            ) from error

        self._validate_kraken_payload(
            payload
        )

        return payload

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
            str(pair)
            .strip()
            .upper()
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

    @classmethod
    def _extract_assets(
        cls,
        web_socket_name: str,
        pair_data: dict[str, Any],
    ) -> tuple[str, str]:
        if "/" in web_socket_name:
            parts = web_socket_name.split(
                "/",
                maxsplit=1,
            )

            base_asset = cls._normalize_asset(
                parts[0]
            )

            quote_asset = cls._normalize_asset(
                parts[1]
            )

            return (
                base_asset,
                quote_asset,
            )

        base_asset = cls._normalize_asset(
            str(
                pair_data.get(
                    "base",
                    "",
                )
            )
        )

        quote_asset = cls._normalize_asset(
            str(
                pair_data.get(
                    "quote",
                    "",
                )
            )
        )

        return (
            base_asset,
            quote_asset,
        )

    @classmethod
    def _normalize_asset(
        cls,
        asset: str,
    ) -> str:
        normalized = (
            asset
            .strip()
            .upper()
        )

        return cls.ASSET_ALIASES.get(
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

            unique_markets.setdefault(
                pair,
                market,
            )

        return list(
            unique_markets.values()
        )



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
    candles = [
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
        for row in dataframe.itertuples(
            index=False
        )
    ]

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
    print(
        json.dumps(
            response,
            separators=(",", ":"),
            ensure_ascii=False,
            allow_nan=False,
        ),
        flush=True,
    )



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
        default=KrakenMarketData.DEFAULT_PAIR,
        help=(
            "Kraken REST pair. "
            "Default: ETHUSD."
        ),
    )

    parser.add_argument(
        "--interval",
        type=int,
        default=KrakenMarketData.DEFAULT_INTERVAL,
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

    try:
        with KrakenMarketData() as service:
            if args.mode == "metadata":
                response = service.get_metadata()

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