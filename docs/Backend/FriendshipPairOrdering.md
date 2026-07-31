# Friendship Pair Ordering

A friendship is one row for two people, with no direction. The pair (A, B) and the pair (B, A) mean exactly the same thing. Storing both would be a duplicate friendship, so only one is allowed to exist.

## How the single row is enforced

Two pieces work together.

* Composite primary key `(UserId1, UserId2)` on Friendships.
* Check constraint `CK_Friendships_UserOrder`, requiring `UserId1 < UserId2`.

The primary key alone is not enough. It stops the exact same row being inserted twice, but it happily accepts `(A, B)` and `(B, A)` as two different rows, because as far as the key is concerned those are different values.

The check constraint is what closes that. Since only the smaller id can sit in `UserId1`, there is exactly one legal arrangement of any pair, so the primary key then has only one row it can possibly collide with. Together they make a duplicate friendship impossible at the database level.

The cost is that every caller has to sort the pair before touching the table. `FriendshipRepository.Canonical` does this, so services can pass the two ids in any order.

## The subtle part: what "smaller" means

`CK_Friendships_UserOrder` is evaluated by Postgres, using Postgres's ordering for the `uuid` type, which is a plain comparison of the 16 raw bytes.

The repository sorts the pair in C#. So the ordering C# picks must agree with the ordering Postgres uses. If they ever disagreed, the repository would write a row it considers correctly ordered and Postgres would reject it outright with a check constraint violation. Not a subtle data bug, a hard insert failure on some fraction of friendships.

This is worth stating explicitly because .NET has more than one plausible way to order a Guid, and only one of them is guaranteed to be the byte ordering:

* `Guid.CompareTo` compares the structure field by field, `_a`, `_b`, `_c`, then the remaining bytes.
* Comparing `ToByteArray(bigEndian: true)` lexicographically is, by construction, exactly the byte ordering Postgres uses.

`FriendshipRepository.CompareAsUuid` deliberately uses the second one.

## Verified behavior

On .NET 10, the two orderings agree. Sweeping 500,000 random Guid pairs and comparing the sign of `Guid.CompareTo` against the sign of the big-endian byte comparison produced zero disagreements. `Guid.CompareTo` treats the leading fields as unsigned, so it does not flip on a high bit the way it is sometimes claimed to.

That means `CompareAsUuid` is not currently fixing a live bug. It is not a redundant safety check either, since only one comparison ever actually runs. It is a choice of the primitive whose correctness is self evident rather than the one that happens to be correct.

The reason to keep it:

* It matches what Postgres does by construction, so it stays correct without depending on the internals of `Guid.CompareTo` staying the way they are today.
* A reader can confirm the C# ordering matches the check constraint by looking at the method, without having to go and verify BCL behavior.


