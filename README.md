# RabinTransform-CSharp

A small C# project that demonstrates a **Rabin-based transform**: generate RSA-like modulus `n = p·q` (with `p, q ≡ 3 (mod 4)`), compute `c = m² mod n`, extract the four square roots of `c` modulo `n`, and return the XOR of those four roots as a derived value.

This implementation is **educational** and uses short variable names (single-letter names) as requested. **Do not** use it in production without a security review and proper cryptographic hardening.

---

## Features

* Generate primes `p` and `q` of chosen bit-length (example uses 512 bits each).
* Compute `n = p * q`.
* Compute `c = m² mod n` for a given `m` (`0 < m < n`).
* Compute the four square roots of `c` (Rabin decryption step).
* Return `XOR(r1, r2, r3, r4)` as a `BigInteger` (or byte array if you adapt it).

Use-cases (experimental / research):

* Hash-like transform based on Rabin hardness.
* Key-derivation experimenting (combine with a KDF/HKDF after XOR).
* Academic demonstration of Rabin root properties.



---





## Security notes & caveats

* **Educational only.** This implementation is *not* a drop-in cryptographic primitive.
* The output `XOR(r1,r2,r3,r4)` is **not** a standard construction with a proven security reduction. Treat results as experimental.
* For real cryptography use **well-vetted** libraries and KDFs (HKDF, PBKDF2, Argon2, libsodium, BouncyCastle).
* When using `BigInteger.ToByteArray()` note it is **little-endian** and may include a sign byte. If you want a fixed-length **positive** result, convert to a fixed-length big-endian byte array and ensure the highest bit is handled (prepend a zero byte if necessary).
* Use large key sizes for real security. The demo uses `512`-bit primes (for speed/testing). For any real security goal, use at least `2048`-bit `n` (i.e., `p,q >= 1024` bits) and vetted prime generation.

---

## Improvements you might want

* Return a fixed-length `byte[]` (big-endian, unsigned) instead of `BigInteger` to avoid sign ambiguity.
* Apply a proper KDF (HKDF) to the XOR result to derive symmetric keys of required length.
* Use tested prime-generation and randomness providers from a reputable crypto library.
* Add deterministic input encoding (e.g., `m = Hash(input)` truncated to `< n`) when deriving from arbitrary keys or strings.
* Store `p` & `q` securely (if used) or discard them and keep only `n` if you only need the hardness assumption.


