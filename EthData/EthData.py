
import requests
import pandas as pd


url = "https://api.binance.com/api/v3/klines"

params = {
    "symbol": "ETHUSDT",
    "interval": "1d",
    "limit": 30
}

response = requests.get(
    url,
    params=params,
    timeout=20
)

response.raise_for_status()

data = response.json()


columns = [
    "open_time",
    "open",
    "high",
    "low",
    "close",
    "volume",
    "close_time",
    "quote_volume",
    "number_of_trades",
    "taker_buy_base_volume",
    "taker_buy_quote_volume",
    "ignore"
]

df = pd.DataFrame(data, columns=columns)


# Convert timestamp
df["date"] = pd.to_datetime(
    df["open_time"],
    unit="ms",
    utc=True
)


# Convert price/volume columns to numbers
numeric_columns = [
    "open",
    "high",
    "low",
    "close",
    "volume"
]

df[numeric_columns] = df[numeric_columns].astype(float)


# Keep only what we currently care about
df = df[
    [
        "date",
        "open",
        "high",
        "low",
        "close",
        "volume"
    ]
]


print(df)