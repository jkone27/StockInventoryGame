## A traditional approach (Transactional DB) - done

Good things: Transactions.
Create an API backed by SQL for products and inventory, 
where products table has many to many relationship,
so an intermediate table must exist to match this relationship.
This approach would work, in most cases, but would slow down development, 
and soon require db and SQL specific knowledge as the database size grows, 
adding properties, adding indexes, adding queries, 
updating a SQL schema is a time consuming operation.
On the other hand a lot of professionals have knowledge and expertise around this approach

## A Document (json) DB approach

This approach is also quite "known", would offer much more flexibility, 
is possibly better, but would suffer the fact that if we wanted to query the db in the future 
or explore it, documents are not really made for this purpose. 
Another aspect is possible inconsitent data could reside in database as being schemaless 
makes it more exposed to be corrupted some time soneer. 
Data sharding is also another complexity of mongodb, 
and eventual inconsistency between shards once the size grows. 
Another issue is the lack of transactions.. 
so some eventual consistency approach should be used to handle concurrency and scaling, 
not so easy/simple to do in practice.

## A Graph DB approach (e.g Neo4j)

if we had a graph database we could query our product and inventory relations, 
most operations would be constant in time or grow logaritmically with the dept of the graph, 
which is also acceptable, not much influenced on the size of the graph (fb approach..)

(PRODUCT 1)-has4-->(ART_1)
(PRODUCT 1)-has2-->(ART_4)
(PRODUCT 1)-has10-->(ART_1)
(PRODUCT 2)-has10-->(ART_1)
(PRODUCT 4)-has10-->(ART_2)

it has advantages of both document db and transactional db, 
neo4j supports transactions and big data size.
Also comes with a UI "by default" in the management, which is pretty nice.