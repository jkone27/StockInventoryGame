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

DROP TABLE test.products_articles;
DROP TABLE test.articles;
DROP TABLE test.products;

INSERT INTO test.articles (id,art_name,stock)
VALUES
    (1,'test_article',4),
    (2,'shoes',2);

INSERT INTO test.articles (id,art_name,stock)
VALUES
    (1,'test_article',-1); --check validation

SELECT * FROM test.articles;

INSERT INTO test.products (prd_name)
VALUES
    ('gift_box_1'),
    ('gift_box_2'),
    ('gift_box_3');

SELECT * FROM test.products;

INSERT INTO test.products_articles(product_id, article_id, qty)
VALUES (1,1,5), (1,2,2);

INSERT INTO test.products_articles(product_id, article_id, qty)
VALUES (1,1,-1); --check validation

SELECT * FROM test.products_articles;

SELECT p.id, p.prd_name, a.art_name, pa.qty, p.is_sold
FROM test.products as p
LEFT JOIN test.products_articles as pa
    ON pa.product_id = p.id 
LEFT JOIN test.articles as a
    ON pa.article_id = a.id;




INSERT INTO test.products(prd_name)
VALUES ('test-select') RETURNING id;


DELETE FROM test.products_articles;
DELETE FROM test.articles;
DELETE FROM test.products;
