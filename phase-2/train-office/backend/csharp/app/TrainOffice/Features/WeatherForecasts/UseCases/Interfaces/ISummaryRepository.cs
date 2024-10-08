using TrainOffice.Infrastructures.DataModels;

namespace TrainOffice.Features.WeatherForecasts.UseCases.Interfaces;

public interface ISummaryRepository
{
    Task<IEnumerable<Summary>> GetSummariesAsync();
}