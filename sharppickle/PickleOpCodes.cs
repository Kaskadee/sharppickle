namespace sharppickle {
    /// <summary>
    ///     Provides an enumeration of all op-codes, which were introduced up to the highest supported pickle protocol (3).
    /// </summary>
    public enum PickleOpCodes : byte {
        /// <summary>
        ///     Push special mark object on stack.
        /// </summary>
        Mark = (byte)'(',
        /// <summary>
        ///     Every pickle ends with STOP.
        /// </summary>
        Stop = (byte)'.',
        /// <summary>
        ///     Discard top most stack item.
        /// </summary>
        Pop = (byte)'0',
        /// <summary>
        ///     Discard stack top through top most mark object.
        /// </summary>
        PopMark = (byte)'1',
        /// <summary>
        ///     Duplicate top stack item.
        /// </summary>
        Dup = (byte)'2',
        /// <summary>
        ///     Push float object; decimal string argument.
        /// </summary>
        Float = (byte)'F',
        /// <summary>
        ///     Push integer or bool; decimal string argument.
        /// </summary>
        Int = (byte)'I',
        /// <summary>
        ///     Push four-byte signed int.
        /// </summary>
        BinInt = (byte)'J',
        /// <summary>
        ///     Push 1-byte unsigned int. 
        /// </summary>
        BinInt1 = (byte)'K',
        /// <summary>
        ///     Push long; decimal string argument.
        /// </summary>
        Long = (byte)'L',
        /// <summary>
        ///     Push 2-byte unsigned int.
        /// </summary>
        BinInt2 = (byte)'M',
        /// <summary>
        ///     Push None.
        /// </summary>
        None = (byte)'N',
        /// <summary>
        ///     Push persistent object; id is taken from string.
        /// </summary>
        PersId = (byte)'P',
        /// <summary>
        ///     Push persistent object; id is taken from stack.
        /// </summary>
        BinPersId = (byte)'Q',
        /// <summary>
        ///     Apply callable to argtuple; both on stack.
        /// </summary>
        Reduce = (byte)'R',
        /// <summary>
        ///     Push string; NL-terminated string argument.
        /// </summary>
        String = (byte)'S',
        /// <summary>
        ///     Push string; counted binary string argument.
        /// </summary>
        BinString = (byte)'T',
        /// <summary>
        ///     Push string; counted binary string argument less than 256 bytes.
        /// </summary>
        ShortBinString = (byte)'U',
        /// <summary>
        ///     Push Unicode string; raw-unicoded-escape'd argument.
        /// </summary>
        Unicode = (byte)'V',
        /// <summary>
        ///     Push Unicode string; counted UTF-8 string argument.
        /// </summary>
        BinUnicode = (byte)'X',
        /// <summary>
        ///     Append stack top to list below it.
        /// </summary>
        Append = (byte)'a',
        /// <summary>
        ///     Call __setstate__ or __dict__.update()
        /// </summary>
        Build = (byte)'b',
        /// <summary>
        ///     Push self.find_class(modname, name); 2 string args.
        /// </summary>
        Global = (byte)'c',
        /// <summary>
        ///     Build a dict from stack items.
        /// </summary>
        Dict = (byte)'d',
        /// <summary>
        ///     Push empty dict.
        /// </summary>
        EmptyDict = (byte)'}',
        /// <summary>
        ///     Extend list on stack by top most stack slice.
        /// </summary>
        Appends = (byte)'e',
        /// <summary>
        ///     Push item from memo on stack; index is string arg.
        /// </summary>
        Get = (byte)'g',
        /// <summary>
        ///     Push item from memo on stack; index is 1-byte arg.
        /// </summary>
        BinGet = (byte)'h',
        /// <summary>
        ///     Build & push class instance.
        /// </summary>
        Inst = (byte)'i',
        /// <summary>
        ///     Push item from memo on stack; index is 4-byte arg.
        /// </summary>
        LongBinGet = (byte)'j',
        /// <summary>
        ///     Build list from top most stack items.
        /// </summary>
        List = (byte)'l',
        /// <summary>
        ///     Push empty list.
        /// </summary>
        EmptyList = (byte)']',
        /// <summary>
        ///     Build & push class instance.
        /// </summary>
        Obj = (byte)'o',
        /// <summary>
        ///     Store stack top in memo; index is string arg.
        /// </summary>
        Put = (byte)'p',
        /// <summary>
        ///     Store stack top in memo; index is 1-byte arg.
        /// </summary>
        BinPut = (byte)'q',
        /// <summary>
        ///     Store stack top in memo; index is 4-byte arg.
        /// </summary>
        LongBinPut = (byte)'r',
        /// <summary>
        ///     Add key + value pair to dict.
        /// </summary>
        SetItem = (byte)'s',
        /// <summary>
        ///     Build tuple from top most stack items.
        /// </summary>
        Tuple = (byte)'t',
        /// <summary>
        ///     Push empty tuple.
        /// </summary>
        EmptyTuple = (byte)')',
        /// <summary>
        ///     Modify dict by adding top most key + value pairs.
        /// </summary>
        SetItems = (byte)'u',
        /// <summary>
        ///     Push float; arg is 8-byte float encoding.
        /// </summary>
        BinFloat = (byte)'G',

        #region Protocol 2.x

        /// <summary>
        ///     Identify pickle protocol.
        /// </summary>
        Proto = (byte)'\x80',
        /// <summary>
        ///     Build object by applying cls.__new__ to argtuple.
        /// </summary>
        NewObj = (byte)'\x81',
        /// <summary>
        ///     Push object from extensions registry; 1-byte index.
        /// </summary>
        Ext1 = (byte)'\x82',
        /// <summary>
        ///     Push object from extensions registry; 2-byte index.
        /// </summary>
        Ext2 = (byte)'\x83',
        /// <summary>
        ///     Push object from extensions registry; 4-byte index.
        /// </summary>
        Ext4 = (byte)'\x84',
        /// <summary>
        ///     Build 1-tuple from stack top.
        /// </summary>
        Tuple1 = (byte)'\x85',
        /// <summary>
        ///     Build 2-tuple from two topmost stack items.
        /// </summary>
        Tuple2 = (byte)'\x86',
        /// <summary>
        ///     Build 3-tuple from three topmost stack items.
        /// </summary>
        Tuple3 = (byte)'\x87',
        /// <summary>
        ///     Push True.
        /// </summary>
        NewTrue = (byte)'\x88',
        /// <summary>
        ///     Push False.
        /// </summary>
        NewFalse = (byte)'\x89',
        /// <summary>
        ///     Push long from less than 256 bytes.
        /// </summary>
        Long1 = (byte)'\x8a',
        /// <summary>
        ///     Push really big long.
        /// </summary>
        Long4 = (byte)'\x8b',

        #endregion

        #region Protocol 3.x

        /// <summary>
        ///     Push bytes to the top of the stack; counted binary string argument.
        /// </summary>
        BinaryBytes = (byte)'B',
        /// <summary>
        ///     Push bytes to the top of the stack; counted binary string argument, but only up to <see cref="byte.MaxValue"/> bytes.
        /// </summary>
        ShortBinaryBytes = (byte)'C',

        #endregion
    }
}
