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

--test select product
SELECT p.id, p.prd_name, a.id, a.art_name, pa.qty, p.is_sold
FROM test.products as p
LEFT JOIN test.products_articles as pa
    ON pa.product_id = p.id 
LEFT JOIN test.articles as a
    ON pa.article_id = a.id;

--cleanup scripts
DELETE FROM test.products_articles;
DELETE FROM test.articles;
DELETE FROM test.products;
