using System;

namespace ChatCommands.Utils;

internal static class ExceptionExtensions
{
    public static Exception GetInnerMostException(this Exception ex) {
        while (ex.InnerException is not null && ex is not null) {
            ex = ex.InnerException;
        }
        return ex;
    }
}