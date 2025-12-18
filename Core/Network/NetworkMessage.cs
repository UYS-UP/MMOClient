using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;


// 通用响应数据结构
[MessagePackObject]
public class ResponseMessage<T>
{
    [Key(0)] public StateCode Code;    // 状态码
    [Key(1)] public T Data;           // 响应数据
    [Key(2)] public string Message;

    // 成功响应快捷方法
    public static ResponseMessage<T> Success(T data, string message = "")
    {
        return new ResponseMessage<T>
        {
            Code = StateCode.Success,
            Data = data,
            Message = message
        };
    }

    // 失败响应快捷方法
    public static ResponseMessage<T> Fail(string message = "", StateCode code = StateCode.BadRequest)
    {
        return new ResponseMessage<T>
        {
            Code = code,
            Data = default(T),
            Message = message
        };
    }
}