using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.Services;

namespace SkillHub.Infrastructure.Services;

public class PdfGenerator : IPdfGenerator
{
    static PdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateCertificateAsync(CertificateModel model, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

                page.Header()
                    .BorderBottom(2)
                    .BorderColor(Colors.Blue.Darken2)
                    .PaddingBottom(10)
                    .Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(model.CenterName).FontSize(24).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().Text("Centro de Formación y Talleres").FontSize(11).Italic();
                        });
                    });

                page.Content().PaddingVertical(25).Column(column =>
                {
                    column.Spacing(15);

                    column.Item().AlignCenter().Text(model.Title)
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    column.Item().PaddingTop(10).Text(text =>
                    {
                        text.Span("Por medio de la presente, se certifica que el/la participante ");
                        text.Span(model.ParticipantName).Bold();
                        text.Span(" con correo ");
                        text.Span(model.ParticipantEmail).Bold();
                        text.Span(" se encuentra formalmente inscrito/a en el taller presencial que se detalla a continuación:");
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(15).Column(details =>
                    {
                        details.Spacing(8);
                        details.Item().Text($"Código del Taller: {model.WorkshopCode}").Bold();
                        details.Item().Text($"Título del Taller: {model.WorkshopTitle}").Bold();
                        details.Item().Text($"Instructor a Cargo: {model.InstructorName}");
                        details.Item().Text($"Fecha y Hora de Inicio: {model.StartDate:yyyy-MM-dd HH:mm} UTC");
                        details.Item().Text($"Fecha y Hora de Fin: {model.EndDate:yyyy-MM-dd HH:mm} UTC");
                        details.Item().Text($"Fecha de Inscripción: {model.EnrollmentDate:yyyy-MM-dd HH:mm} UTC");
                    });

                    column.Item().PaddingTop(15).Text("La presente constancia certifica la validez de la inscripción activa en el sistema oficial de SkillHub.")
                        .FontSize(10)
                        .Italic();
                });

                page.Footer()
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingTop(10)
                    .Row(row =>
                    {
                        row.RelativeItem().Text($"Fecha de emisión: {model.GeneratedAt:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        row.RelativeItem().AlignRight().Text("SkillHub — Sistema de Gestión")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });
            });
        });

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }
}
