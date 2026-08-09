using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Application.Features.Appointments.Commands.CompleteAppointment;
using Rafiq.Infrastructure.Persistence;

public class Test
{
    public static async Task Run()
    {
        Console.WriteLine("Testing handler...");
    }
}
Test.Run().GetAwaiter().GetResult();
