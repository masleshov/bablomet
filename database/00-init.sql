create extension if not exists "uuid-ossp";

create table instruments
(
    symbol text primary key,
    shortname text,
    description text,
    exchange text not null,
    type text,
    lot_size integer not null,
    face_value numeric(32, 18) not null,
    cfi_code text not null,
    cancellation bigint not null,
    min_step numeric(32, 18) not null,
    rating bigint not null,
    margin_buy numeric(32, 18) not null,
    margin_sell numeric(32, 18) not null,
    margin_rate numeric(32, 18) not null,
    price_step numeric(32, 18) not null,
    price_max numeric(32, 18) not null,
    price_min numeric(32, 18) not null,
    theor_price numeric(32, 18) not null,
    theor_price_limit numeric(32, 18) not null,
    volatility numeric(32, 18) not null,
    currency text,
    isin text,
    yield numeric(32, 18),
    primary_board text,
    trading_status integer not null,
    trading_status_info text,
    complex_product_category text
);

create table bars
(
    bar_id serial primary key,
    symbol text not null references instruments(symbol),
    time_frame text not null,
    time bigint not null,
    close numeric(32, 18) not null,
    open numeric(32, 18) not null,
    high numeric(32, 18) not null,
    low numeric(32, 18) not null,
    volume bigint not null
);

create type indicator_type_et as enum ( 'SMA', 'EMA', 'MACD', 'VWAP' );
create table telegram_bot_subscriptions
(
    subscription_id serial primary key,
    indicator_type indicator_type_et not null,
    symbol text not null references instruments(symbol),
    time_frame text not null,
    parameters integer[],
    chat_id bigint not null
);