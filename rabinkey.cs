using System;
using System.Numerics;
using System.Security.Cryptography;

class RabinXor
{
    // Miller-Rabin probabilistic primality test
    static bool IsProbablePrime(BigInteger n, int k = 12)
    {
        if (n < 2) return false;
        var small = new int[] {2,3,5,7,11,13,17,19,23,29,31};
        foreach (var sp in small)
        {
            if (n == sp) return true;
            if (n % sp == 0) return false;
        }

        BigInteger d = n - 1;
        int s = 0;
        while ((d & 1) == 0) { d >>= 1; s++; }

        using (var rng = RandomNumberGenerator.Create())
        {
            int bytes = n.GetByteCount();
            byte[] buf = new byte[bytes];
            for (int i = 0; i < k; i++)
            {
                BigInteger a;
                do
                {
                    rng.GetBytes(buf);
                    a = new BigInteger(buf);
                    if (a < 0) a = -a;
                } while (a < 2 || a >= n - 2);

                BigInteger x = BigInteger.ModPow(a, d, n);
                if (x == 1 || x == n - 1) continue;

                bool comp = true;
                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, n);
                    if (x == n - 1) { comp = false; break; }
                }
                if (comp) return false;
            }
        }
        return true;
    }

    // Generate prime of 'bits' length with p % 4 == 3
    static BigInteger GenPrime(int bits)
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            int bytes = (bits + 7) / 8;
            byte[] b = new byte[bytes];
            while (true)
            {
                rng.GetBytes(b);
                b[bytes - 1] |= 0x80; // ensure top bit set
                b[0] |= 1; // make odd
                BigInteger p = new BigInteger(b);
                if (p < 0) p = -p;

                // adjust to p % 4 == 3
                int r = (int)(p % 4);
                if (r == 0) p += 3;
                else if (r == 1) p += 2;
                else if (r == 2) p += 1;

                if (IsProbablePrime(p)) return p;
            }
        }
    }

    // Extended GCD -> returns (g, x, y) such that ax + by = g
    static (BigInteger g, BigInteger x, BigInteger y) Egcd(BigInteger a, BigInteger b)
    {
        if (b == 0) return (a, 1, 0);
        var (g, x1, y1) = Egcd(b, a % b);
        return (g, y1, x1 - (a / b) * y1);
    }

    // modular inverse a^{-1} mod m  (assumes gcd(a,m)=1)
    static BigInteger Inv(BigInteger a, BigInteger m)
    {
        var (g, x, y) = Egcd(a, m);
        if (g != 1 && g != -1) throw new ArgumentException("No inverse");
        x %= m;
        if (x < 0) x += m;
        return x;
    }

    // compute four square roots of c modulo n (p,q ≡ 3 (mod 4))
    // returns array [r1, r2, r3, r4]
    static BigInteger[] Roots(BigInteger c, BigInteger p, BigInteger q)
    {
        BigInteger n = p * q;
        BigInteger mp = BigInteger.ModPow(c, (p + 1) / 4, p);
        BigInteger mq = BigInteger.ModPow(c, (q + 1) / 4, q);

        // CRT coefficients
        BigInteger invQmodP = Inv(q % p, p); // q * invQmodP ≡ 1 (mod p)
        BigInteger invPmodQ = Inv(p % q, q); // p * invPmodQ ≡ 1 (mod q)

        BigInteger a1 = q * invQmodP; // ≡1 mod p, 0 mod q
        BigInteger a2 = p * invPmodQ; // ≡0 mod p, 1 mod q

        // combine to get root congruent to (mp mod p, mq mod q)
        BigInteger r1 = (mp * a1 + mq * a2) % n;
        if (r1 < 0) r1 += n;
        BigInteger r2 = n - r1; // -r1 mod n

        // combine to get root congruent to (-mp mod p, mq mod q)
        BigInteger mpNeg = (p - mp) % p;
        BigInteger r3 = (mpNeg * a1 + mq * a2) % n;
        if (r3 < 0) r3 += n;
        BigInteger r4 = n - r3;

        return new BigInteger[] { r1, r2, r3, r4 };
    }

    // XOR of four BigIntegers -> return BigInteger
    static BigInteger XorFour(BigInteger[] arr, int nBytes)
    {
        byte[][] bs = new byte[4][];
        for (int i = 0; i < 4; i++)
        {
            bs[i] = arr[i].ToByteArray(); // little-endian, two's complement
            // pad to nBytes
            if (bs[i].Length < nBytes)
            {
                var nb = new byte[nBytes];
                Array.Copy(bs[i], nb, bs[i].Length);
                bs[i] = nb;
            }
            else if (bs[i].Length > nBytes)
            {
                // truncate possible sign byte beyond nBytes
                var nb = new byte[nBytes];
                Array.Copy(bs[i], 0, nb, 0, nBytes);
                bs[i] = nb;
            }
        }

        byte[] xr = new byte[nBytes];
        for (int i = 0; i < nBytes; i++)
        {
            xr[i] = (byte)(bs[0][i] ^ bs[1][i] ^ bs[2][i] ^ bs[3][i]);
        }
        return new BigInteger(xr); // little-endian => non-negative if highest bit zero; may be negative if top bit 1, but acceptable
    }

    // The transform: input m (0 < m < n) -> XOR of four roots
    static BigInteger RabinTransform(BigInteger m, BigInteger p, BigInteger q)
    {
        BigInteger n = p * q;
        if (m <= 0 || m >= n) throw new ArgumentException("m must be in (0, n)");
        BigInteger c = BigInteger.ModPow(m, 2, n);
        var rs = Roots(c, p, q);
        int nb = n.GetByteCount();
        return XorFour(rs, nb);
    }

    static void Main()
    {
        int bits = 512;
        Console.WriteLine("Generating p and q (" + bits + " bits each) ...");
        BigInteger p = GenPrime(bits);
        BigInteger q = GenPrime(bits);
        while (p == q) q = GenPrime(bits);
        BigInteger n = p * q;

        Console.WriteLine($"p bits: {p.GetBitLength()}, q bits: {q.GetBitLength()}, n bits: {n.GetBitLength()}");

        // generate random m in (1, n-1)
        BigInteger m;
        using (var rng = RandomNumberGenerator.Create())
        {
            int nb = n.GetByteCount();
            byte[] b = new byte[nb];
            do
            {
                rng.GetBytes(b);
                m = new BigInteger(b);
                if (m < 0) m = -m;
                m %= (n - 2);
                m += 2;
            } while (m <= 1 || m >= n);
        }

        Console.WriteLine("m (hex): " + BitConverter.ToString(m.ToByteArray()).Replace("-", ""));
        BigInteger x = RabinTransform(m, p, q);
        Console.WriteLine("RabinTransform(m) (hex): " + BitConverter.ToString(x.ToByteArray()).Replace("-", ""));

        // optionally show the four roots and their xor check
        BigInteger c = BigInteger.ModPow(m, 2, n);
        var roots = Roots(c, p, q);
        Console.WriteLine("Roots (hex):");
        foreach (var r in roots) Console.WriteLine(BitConverter.ToString(r.ToByteArray()).Replace("-", ""));
        int nbBytes = n.GetByteCount();
        BigInteger check = XorFour(roots, nbBytes);
        Console.WriteLine("XOR(roots) == RabinTransform? " + (check == x));
    }
}

// small extension for bit-length
static class BigIntExt
{
    public static int GetBitLength(this BigInteger v)
    {
        if (v == 0) return 1;
        v = BigInteger.Abs(v);
        byte[] b = v.ToByteArray();
        int msb = b[b.Length - 1];
        int bits = (b.Length - 1) * 8;
        while (msb != 0) { bits++; msb >>= 1; }
        return bits;
    }
}
