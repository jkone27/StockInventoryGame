CREATE EXTENSION IF NOT EXISTS "uuid-ossp"; --generate uuid
CREATE EXTENSION IF NOT EXISTS "citext"; --case insensitive varchar

CREATE SCHEMA test;

CREATE TABLE test.articles (
    id BIGINT PRIMARY KEY NOT NULL, --could be string to be extensible
    art_name CITEXT UNIQUE NOT NULL, --creates unique index
    stock BIGINT NOT NULL CHECK (stock >= 0)
);

CREATE TABLE test.products (
    id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    is_sold BOOLEAN NOT NULL DEFAULT false, --to archive sold products (we dont want to delete them from DB)
    prd_name CITEXT UNIQUE NOT NULL --creates unique index
);

CREATE TABLE test.products_articles (
    product_id BIGINT REFERENCES test.products(id) ON UPDATE CASCADE,
    article_id BIGINT REFERENCES test.articles(id) ON UPDATE CASCADE,
    qty INT NOT NULL CHECK (qty >= 0),
    PRIMARY KEY(product_id,article_id)
);

--clean schema

DROP SCHEMA test;
DROP TABLE test.products_articles;
DROP TABLE test.articles;
DROP TABLE test.products;