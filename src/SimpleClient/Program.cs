﻿
using System.Net;

using Shared;
using Shared.Messages;

var application = new NetworkApplicationBuilder()
    .Use(async (c, next, ct) => {
        if(next is null){
            // todo add diagnostic message;
            return;
        }
        try{
            await next(c,ct);
        }catch(Exception ex){
            Console.Error.WriteLine(ex.Message);
        }
    })
    .Use(async (con, ct) => {
        while(!ct.IsCancellationRequested){
            Console.WriteLine("Select operation");
            Console.WriteLine("1. SUM");
            Console.WriteLine("2. MAX");
            Console.WriteLine("3. MIN");
            int operation;
            while(int.TryParse(Console.ReadLine(), out operation) && operation is <1 or >3){
                Console.WriteLine("Invalid operation");
            }

            List<double> operands = new();

            while(double.TryParse(Console.ReadLine(), out var it)){
                operands.Add(it);
            }

            await con.Send<Message>(new MathOperationRequestMessage{
                Method = operation switch{
                    1 => "SUM",
                    2 => "MAX",
                    3 => "MIN",
                    _ => throw new UnsupportedException()
                },
                Operands = operands.ToArray()
            }, XmlChannel.Encode);

            var response = await con.Receive(XmlChannel.Decode);

            if(response is MathOperationResponseMessage mathOperationResponse){
                Console.WriteLine($"Result is : {mathOperationResponse.Result}");
            }
            else if(response is NotSupportedOperationMessage notSupported){
                Console.WriteLine("Server not supported this type of operations");
            }
        }
    })
    .Build();

Connection clientConnection = null!;
do {
    try {
        Console.WriteLine("Try connect to server");
        var connection = await Connection.ConnectAsync(IPEndPoint.Parse("127.0.0.1:8888"));
        clientConnection = connection;
    }catch(Exception ex ) { 
        Console.Error.WriteLine(ex.ToString());
    }
    await Task.Delay(1000);
} while( clientConnection is null );

await application(clientConnection, default);