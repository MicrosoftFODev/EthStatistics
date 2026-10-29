import requests
import pandas as pd


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

        params = {
            "pair": pair.strip().upper(),
            "interval": interval
        }

        response = requests.get(
            self.BASE_URL,
            params=params,
            timeout=30,
            headers={
                "User-Agent": "CryptoStatistics/1.0"
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
            for key in result.keys()
            if key != "last"
        ]

        if not pair_keys:
            raise RuntimeError(
                "Kraken returned no OHLC data."
            )

        result_pair = pair_keys[0]
        candles = result[result_pair]

        columns = [
            "timestamp",
            "open",
            "high",
            "low",
            "close",
            "vwap",
            "volume",
            "trade_count"
        ]

        df = pd.DataFrame(
            candles,
            columns=columns
        )

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

        df[numeric_columns] = (
            df[numeric_columns]
            .apply(pd.to_numeric, errors="raise")
        )

        df["trade_count"] = pd.to_numeric(
            df["trade_count"],
            errors="raise"
        ).astype("int64")

        df = df[
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

        df = (
            df
            .sort_values("date")
            .reset_index(drop=True)
        )

        return df


if __name__ == "__main__":
    market_data = KrakenMarketData()

    try:
        data = market_data.get_ohlcv(
            pair="ETHUSD",
            interval=1440
        )

        print(data.to_string(index=False))

        if not data.empty:
            print()
            print(f"Rows:       {len(data)}")
            print(f"First date: {data['date'].iloc[0]}")
            print(f"Last date:  {data['date'].iloc[-1]}")

    except requests.exceptions.ConnectionError as error:
        print("Could not connect to Kraken.")
        print(error)

    except requests.exceptions.Timeout:
        print("The Kraken request timed out.")

    except requests.exceptions.HTTPError as error:
        print("Kraken returned an HTTP error.")
        print(error)

    except (
        RuntimeError,
        ValueError,
        KeyError,
        TypeError
    ) as error:
        print("Could not process Kraken market data.")
        print(error)