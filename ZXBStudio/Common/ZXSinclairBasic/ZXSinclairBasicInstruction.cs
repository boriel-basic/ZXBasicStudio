﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXBasicStudio.Common.ZXSinclairBasic
{
    public enum ZXSinclairBasicInstruction
    {
        RND = 0xA5,
        INKEYS = 0xA6,
        PI = 0xA7,
        FN = 0xA8,
        POINT = 0xA9,
        SCREENS = 0xAA,
        ATTR = 0xAB,
        AT = 0xAC,
        TAB = 0xAD,
        VALS = 0xAE,
        CODE = 0xAF,
        VAL = 0xB0,
        LEN = 0xB1,
        SIN = 0xB2,
        COS = 0xB3,
        TAN = 0xB4,
        ASN = 0xB5,
        ACS = 0xB6,
        ATN = 0xB7,
        LN = 0xB8,
        EXP = 0xB9,
        INT = 0xBA,
        SQR = 0xBB,
        SGN = 0xBC,
        ABS = 0xBD,
        PEEK = 0xBE,
        IN = 0xBF,
        USR = 0xC0,
        STRS = 0xC1,
        CHRS = 0xC2,
        NOT = 0xC3,
        BIN = 0xC4,
        OR = 0xC5,
        AND = 0xC6,
        LESSEQ = 0xC7,
        GREATEQ = 0xC8,
        DISTINCT = 0xC9,
        LINE = 0xCA,
        THEN = 0xCB,
        TO = 0xCC,
        STEP = 0xCD,
        DEFFN = 0xCE,
        CAT = 0xCF,
        FORMAT = 0xD0,
        MOVE = 0xD1,
        ERASE = 0xD2,
        OPEN = 0xD3,
        CLOSE = 0xD4,
        MERGE = 0xD5,
        VERIFY = 0xD6,
        BEEP = 0xD7,
        CIRCLE = 0xD8,
        INK = 0xD9,
        PAPER = 0xDA,
        FLASH = 0xDB,
        BRIGHT = 0xDC,
        INVERSE = 0xDD,
        OVER = 0xDE,
        OUT = 0xDF,
        LPRINT = 0xE0,
        LLIST = 0xE1,
        STOP = 0xE2,
        READ = 0xE3,
        DATA = 0xE4,
        RESTORE = 0xE5,
        NEW = 0xE6,
        BORDER = 0xE7,
        CONTINUE = 0xE8,
        DIM = 0xE9,
        REM = 0xEA,
        FOR = 0xEB,
        GOTO = 0xEC,
        GOSUB = 0xED,
        INPUT = 0xEE,
        LOAD = 0xEF,
        LIST = 0xF0,
        LET = 0xF1,
        PAUSE = 0xF2,
        NEXT = 0xF3,
        POKE = 0xF4,
        PRINT = 0xF5,
        PLOT = 0xF6,
        RUN = 0xF7,
        SAVE = 0xF8,
        RANDOMIZE = 0xF9,
        IF = 0xFA,
        CLS = 0xFB,
        DRAW = 0xFC,
        CLEAR = 0xFD,
        RETURN = 0xFE,
        COPY = 0xFF
    }
}