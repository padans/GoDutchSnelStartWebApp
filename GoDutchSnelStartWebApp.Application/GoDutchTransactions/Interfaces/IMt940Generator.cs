using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

public interface IMt940Generator
{
    string Generate(Mt940Request request);
}