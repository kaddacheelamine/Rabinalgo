using System;
using System.Numerics;

class Rabin
{
    static BigInteger Inv(BigInteger a, BigInteger m)
    {
        BigInteger m0 = m, x0 = 0, x1 = 1;
        while (a > 1)
        {
            BigInteger q = a / m;
            (a, m) = (m, a % m);
            (x0, x1) = (x1 - q * x0, x0);
        }
        return x1 < 0 ? x1 + m0 : x1;
    }

    static BigInteger Pow(BigInteger b, BigInteger e, BigInteger m)
    {
        BigInteger r = 1;
        while (e > 0)
        {
            if ((e & 1) == 1) r = (r * b) % m;
            b = (b * b) % m;
            e >>= 1;
        }
        return r;
    }

    static void Main()
    {
        // مفاتيح (مثال صغير - لا يُستخدم حقيقياً)
        BigInteger p = 7; 
        BigInteger q = 11;
        BigInteger n = p * q;

        // الرسالة
        BigInteger m = 20;
        Console.WriteLine("m = " + m);

        // التشفير
        BigInteger c = (m * m) % n;
        Console.WriteLine("c = " + c);

        // فك التشفير
        BigInteger mp = Pow(c, (p + 1) / 4, p);
        BigInteger mq = Pow(c, (q + 1) / 4, q);

        BigInteger y = Inv(p, q);
        BigInteger x = Inv(q, p);

        BigInteger r1 = (y * p * mq + x * q * mp) % n;
        BigInteger r2 = n - r1;
        BigInteger r3 = (y * p * mq - x * q * mp) % n;
        BigInteger r4 = n - r3;

        Console.WriteLine("Solutions:");
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        Console.WriteLine(r3);
        Console.WriteLine(r4);
    }
}
