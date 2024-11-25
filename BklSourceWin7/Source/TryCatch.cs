using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class TryCatchExtention
{
    public static Func<T, R> TryCatchWapper<T, R>(this Func<T, R> func)
    {
        return input => TryCatchExtention.TryCatch(func, input);
    }
    public static Tout TryCatch<Tout>(this Func<Tout> func, Tout defaultVal, [CallerMemberName] string str = "")
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
            return defaultVal;
        }
    }
    public static Tout TryCatch<Tout>(this Func<Tout> func, [CallerMemberName] string str = "")
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
            return default(Tout);
        }
    }
    public static Tout TryCatch<Tin, Tout>(this Func<Tin, Tout> func, Tin inparam, [CallerMemberName] string str = "")
    {
        try
        {
            return func(inparam);
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + $"{inparam} " + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
            return default(Tout);
        }
    }
    public static Tout TryCatch<Tin, Tin2, Tout>(this Func<Tin, Tin2, Tout> func, Tin inparam, Tin2 t2, [CallerMemberName] string str = "")
    {
        try
        {
            return func(inparam, t2);
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + $"{inparam}  {t2} " + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
            return default(Tout);
        }
    }
    public static Tout TryCatch<Tin, Tin2, Tin22, Tout>(this Func<Tin, Tin2, Tin22, Tout> func, Tin inparam, Tin2 t2, Tin22 t22, [CallerMemberName] string str = "")
    {
        try
        {
            return func(inparam, t2, t22);
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + $"{inparam} {t2} {t22}" + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
            return default(Tout);
        }
    }
    public static void TryCatch(this Action func, [CallerMemberName] string str = "")
    {
        try
        {
            func();
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
        }
    }
    public static void TryCatch<Tin>(this Action<Tin> func, Tin inparam, [CallerMemberName] string str = "")
    {
        try
        {
            func(inparam);
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + $"{inparam} " + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
        }
    }
    public static void TryCatch<Tin, T2>(this Action<Tin, T2> func, Tin inparam, T2 t2, [CallerMemberName] string str = "")
    {
        try
        {
            func(inparam, t2);
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + $"{inparam} {t2} " + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
        }
    }
    public static void TryCatch<Tin, Tin22, T2>(this Action<Tin, Tin22, T2> func, Tin inparam, Tin22 t22, T2 t2, [CallerMemberName] string str = "")
    {
        try
        {
            func(inparam, t22, t2);
        }
        catch (Exception ex)
        {
            Console.WriteLine("TRYCACHE callMethod:" + str + " errorStack:" + $"{inparam} {t22}  {t2} " + ex.StackTrace + " full:" + ex.ToString());
            // throw ex;
        }
    }
}
