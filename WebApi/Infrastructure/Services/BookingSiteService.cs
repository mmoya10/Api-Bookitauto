using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApi.DTOs.BookingSites;
using WebApi.Infrastructure.Persistence;
using WebApi.Domain.Entities;
using SiteServiceEntity = WebApi.Domain.Entities.BookingSiteService;


namespace WebApi.Infrastructure.Services
{
    public interface IBookingSiteService
    {
        Task<IReadOnlyList<BookingSiteDto>> ListAsync(Guid branchId, CancellationToken ct);
        Task<Guid> CreateAsync(Guid branchId, BookingSiteCreateRequest req, CancellationToken ct);
        Task UpdateAsync(Guid siteId, BookingSiteUpdateRequest req, CancellationToken ct);
        Task DeleteAsync(Guid siteId, CancellationToken ct);

        Task<IReadOnlyList<BookingSiteStepDto>> GetStepsAsync(Guid siteId, CancellationToken ct);
        Task<Guid> AddStepAsync(Guid siteId, string step, int position, CancellationToken ct);
        Task UpdateStepAsync(Guid stepId, string? step, int? position, CancellationToken ct);
        Task DeleteStepAsync(Guid stepId, CancellationToken ct);

        Task<IReadOnlyList<BookingSiteServiceItemDto>> GetSiteServicesAsync(Guid siteId, CancellationToken ct);
        Task AddSiteServiceAsync(Guid siteId, Guid serviceId, bool active, int position, CancellationToken ct);
        Task UpdateSiteServiceAsync(Guid siteId, Guid serviceId, bool? active, int? position, CancellationToken ct);
        Task DeleteSiteServiceAsync(Guid siteId, Guid serviceId, CancellationToken ct);

        Task<IReadOnlyList<BookingFormDto>> GetFormsAsync(Guid siteId, CancellationToken ct);
        Task<Guid> CreateFormAsync(Guid siteId, BookingFormCreateRequest req, CancellationToken ct);
        Task<Guid> AddFieldAsync(Guid formId, BookingFormFieldCreateRequest req, CancellationToken ct);
        Task UpdateFieldAsync(Guid fieldId, BookingFormFieldUpdateRequest req, CancellationToken ct);
        Task DeleteFieldAsync(Guid fieldId, CancellationToken ct);
    }

    public sealed class BookingSiteService : IBookingSiteService
    {
        private readonly AppDbContext _db;
        public BookingSiteService(AppDbContext db) => _db = db;

        // ===== Sites =====
        public async Task<IReadOnlyList<BookingSiteDto>> ListAsync(Guid branchId, CancellationToken ct)
        {
            return await _db.BookingSites
                .AsNoTracking()
                .Where(s => s.BranchId == branchId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new BookingSiteDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Slug = s.Slug,
                    IsPrimary = s.IsPrimary,
                    Visible = s.Visible,
                    Status = s.Status,
                    DefaultFlowOrder = s.DefaultFlowOrder,
                    AllowAutobook = s.AllowAutobook,
                    AutobookRequiresOnlinePayment = s.AutobookRequiresOnlinePayment,
                    AutobookMaxHoursBefore = s.AutobookMaxHoursBefore,
                    MinAdvanceMinutes = s.MinAdvanceMinutes,
                    MaxAdvanceDays = s.MaxAdvanceDays,
                    FormRequired = s.FormRequired,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync(ct);
        }

        public async Task<Guid> CreateAsync(Guid branchId, BookingSiteCreateRequest req, CancellationToken ct)
        {
            ValidateFlowOrder(req.DefaultFlowOrder);

            // slug único por branch
            var slugExists = await _db.BookingSites.AnyAsync(s => s.BranchId == branchId && s.Slug == req.Slug, ct);
            if (slugExists) throw new InvalidOperationException("Slug ya en uso en esta sucursal.");

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            if (req.IsPrimary)
            {
                // desmarcar otros primary
                var prims = await _db.BookingSites.Where(s => s.BranchId == branchId && s.IsPrimary).ToListAsync(ct);
                foreach (var p in prims) p.IsPrimary = false;
            }

            var entity = new BookingSite
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = req.Name.Trim(),
                Description = req.Description,
                Slug = req.Slug.Trim(),
                IsPrimary = req.IsPrimary,
                Visible = req.Visible,
                Status = req.Status,
                DefaultFlowOrder = req.DefaultFlowOrder,
                AllowAutobook = req.AllowAutobook,
                AutobookRequiresOnlinePayment = req.AutobookRequiresOnlinePayment,
                AutobookMaxHoursBefore = req.AutobookMaxHoursBefore,
                MinAdvanceMinutes = req.MinAdvanceMinutes,
                MaxAdvanceDays = req.MaxAdvanceDays,
                FormRequired = req.FormRequired,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.BookingSites.Add(entity);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(Guid siteId, BookingSiteUpdateRequest req, CancellationToken ct)
        {
            var site = await _db.BookingSites.FirstOrDefaultAsync(s => s.Id == siteId, ct)
                ?? throw new KeyNotFoundException("Booking site no encontrado.");

            if (req.DefaultFlowOrder is not null)
                ValidateFlowOrder(req.DefaultFlowOrder);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            if (req.Slug is { Length: > 0 } slug && !slug.Equals(site.Slug, StringComparison.Ordinal))
            {
                var exists = await _db.BookingSites.AnyAsync(s => s.BranchId == site.BranchId && s.Slug == slug && s.Id != siteId, ct);
                if (exists) throw new InvalidOperationException("Slug ya en uso en esta sucursal.");
                site.Slug = slug.Trim();
            }

            if (req.IsPrimary is true)
            {
                // desmarcar otros primary
                var prims = await _db.BookingSites.Where(s => s.BranchId == site.BranchId && s.IsPrimary && s.Id != site.Id).ToListAsync(ct);
                foreach (var p in prims) p.IsPrimary = false;
                site.IsPrimary = true;
            }
            else if (req.IsPrimary is false)
            {
                site.IsPrimary = false;
            }

            if (req.Name is not null) site.Name = req.Name.Trim();
            if (req.Description is not null) site.Description = req.Description;
            if (req.Visible.HasValue) site.Visible = req.Visible.Value;
            if (req.Status is not null) site.Status = req.Status;
            if (req.DefaultFlowOrder is not null) site.DefaultFlowOrder = req.DefaultFlowOrder;
            if (req.AllowAutobook.HasValue) site.AllowAutobook = req.AllowAutobook.Value;
            if (req.AutobookRequiresOnlinePayment.HasValue) site.AutobookRequiresOnlinePayment = req.AutobookRequiresOnlinePayment.Value;
            if (req.AutobookMaxHoursBefore.HasValue) site.AutobookMaxHoursBefore = req.AutobookMaxHoursBefore.Value;
            if (req.MinAdvanceMinutes.HasValue) site.MinAdvanceMinutes = req.MinAdvanceMinutes.Value;
            if (req.MaxAdvanceDays.HasValue) site.MaxAdvanceDays = req.MaxAdvanceDays.Value;
            if (req.FormRequired.HasValue) site.FormRequired = req.FormRequired.Value;
            site.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        public async Task DeleteAsync(Guid siteId, CancellationToken ct)
        {
            var site = await _db.BookingSites.FirstOrDefaultAsync(s => s.Id == siteId, ct)
                ?? throw new KeyNotFoundException("Booking site no encontrado.");

            // En vez de borrar físico, archivar
            site.Status = "archived";
            site.Visible = false;
            site.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // ===== Steps =====
        public async Task<IReadOnlyList<BookingSiteStepDto>> GetStepsAsync(Guid siteId, CancellationToken ct)
        {
            return await _db.BookingSiteSteps
                .AsNoTracking()
                .Where(s => s.SiteId == siteId)
                .OrderBy(s => s.Position)
                .Select(s => new BookingSiteStepDto { Id = s.Id, Step = s.Step, Position = s.Position })
                .ToListAsync(ct);
        }

        public async Task<Guid> AddStepAsync(Guid siteId, string step, int position, CancellationToken ct)
        {
            ValidateStep(step);
            if (position < 0) throw new InvalidOperationException("Position debe ser >= 0.");

            var exists = await _db.BookingSiteSteps.AnyAsync(s => s.SiteId == siteId && s.Step == step, ct);
            if (exists) throw new InvalidOperationException("Ese step ya existe en el sitio.");

            // mover hacia abajo los >= position
            var toShift = await _db.BookingSiteSteps.Where(s => s.SiteId == siteId && s.Position >= position).ToListAsync(ct);
            foreach (var s in toShift) s.Position++;

            var entity = new BookingSiteStep
            {
                Id = Guid.NewGuid(),
                SiteId = siteId,
                Step = step,
                Position = position
            };

            _db.BookingSiteSteps.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateStepAsync(Guid stepId, string? step, int? position, CancellationToken ct)
        {
            var entity = await _db.BookingSiteSteps.FirstOrDefaultAsync(x => x.Id == stepId, ct)
                ?? throw new KeyNotFoundException("Step no encontrado.");

            if (step is not null && !step.Equals(entity.Step, StringComparison.Ordinal))
            {
                ValidateStep(step);
                var dup = await _db.BookingSiteSteps.AnyAsync(s => s.SiteId == entity.SiteId && s.Step == step && s.Id != stepId, ct);
                if (dup) throw new InvalidOperationException("Ese step ya existe en el sitio.");
                entity.Step = step;
            }

            if (position.HasValue && position.Value != entity.Position)
            {
                if (position.Value < 0) throw new InvalidOperationException("Position debe ser >= 0.");

                // Reordenar
                var all = await _db.BookingSiteSteps.Where(s => s.SiteId == entity.SiteId).OrderBy(s => s.Position).ToListAsync(ct);
                all.RemoveAll(s => s.Id == stepId);
                all.Insert(Math.Min(position.Value, all.Count), entity); // insertar en nueva posición

                for (var i = 0; i < all.Count; i++)
                    all[i].Position = i;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteStepAsync(Guid stepId, CancellationToken ct)
        {
            var entity = await _db.BookingSiteSteps.FirstOrDefaultAsync(x => x.Id == stepId, ct)
                ?? throw new KeyNotFoundException("Step no encontrado.");

            var siteId = entity.SiteId;
            var pos = entity.Position;

            _db.BookingSiteSteps.Remove(entity);

            // compactar posiciones
            var rest = await _db.BookingSiteSteps.Where(s => s.SiteId == siteId && s.Position > pos).ToListAsync(ct);
            foreach (var s in rest) s.Position--;

            await _db.SaveChangesAsync(ct);
        }

        // ===== Site Services =====
        public async Task<IReadOnlyList<BookingSiteServiceItemDto>> GetSiteServicesAsync(Guid siteId, CancellationToken ct)
        {
            return await _db.BookingSiteServices
                .AsNoTracking()
                .Where(x => x.SiteId == siteId)
                .OrderBy(x => x.Position)
                .Select(x => new BookingSiteServiceItemDto
                {
                    ServiceId = x.ServiceId,
                    ServiceName = x.Service.Name,
                    Active = x.Active,
                    Position = x.Position
                })
                .ToListAsync(ct);
        }

        public async Task AddSiteServiceAsync(Guid siteId, Guid serviceId, bool active, int position, CancellationToken ct)
        {
            // validar que el service pertenece al mismo branch del site
            var site = await _db.BookingSites.FirstOrDefaultAsync(s => s.Id == siteId, ct)
                ?? throw new KeyNotFoundException("Site no encontrado.");
            var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && s.BranchId == site.BranchId, ct)
                ?? throw new InvalidOperationException("Service no pertenece a esta sucursal.");

            var exists = await _db.BookingSiteServices.AnyAsync(x => x.SiteId == siteId && x.ServiceId == serviceId, ct);
            if (exists) throw new InvalidOperationException("El servicio ya está en el sitio.");

            var toShift = await _db.BookingSiteServices.Where(s => s.SiteId == siteId && s.Position >= position).ToListAsync(ct);
            foreach (var s in toShift) s.Position++;

            _db.BookingSiteServices.Add(new SiteServiceEntity
            {
                SiteId = siteId,
                ServiceId = serviceId,
                Active = active,
                Position = position
            });


            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateSiteServiceAsync(Guid siteId, Guid serviceId, bool? active, int? position, CancellationToken ct)
        {
            var entity = await _db.BookingSiteServices.FirstOrDefaultAsync(x => x.SiteId == siteId && x.ServiceId == serviceId, ct)
                ?? throw new KeyNotFoundException("El servicio no está en el sitio.");

            if (active.HasValue) entity.Active = active.Value;

            if (position.HasValue)
            {
                var list = await _db.BookingSiteServices.Where(s => s.SiteId == siteId).OrderBy(s => s.Position).ToListAsync(ct);
                var it = list.First(x => x.ServiceId == serviceId);
                list.Remove(it);
                list.Insert(Math.Min(position.Value, list.Count), it);
                for (var i = 0; i < list.Count; i++) list[i].Position = i;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteSiteServiceAsync(Guid siteId, Guid serviceId, CancellationToken ct)
        {
            var entity = await _db.BookingSiteServices.FirstOrDefaultAsync(x => x.SiteId == siteId && x.ServiceId == serviceId, ct)
                ?? throw new KeyNotFoundException("El servicio no está en el sitio.");

            var pos = entity.Position;
            _db.BookingSiteServices.Remove(entity);

            var rest = await _db.BookingSiteServices.Where(s => s.SiteId == siteId && s.Position > pos).ToListAsync(ct);
            foreach (var s in rest) s.Position--;

            await _db.SaveChangesAsync(ct);
        }

        // ===== Forms =====
        public async Task<IReadOnlyList<BookingFormDto>> GetFormsAsync(Guid siteId, CancellationToken ct)
        {
            return await _db.BookingForms
                .AsNoTracking()
                .Where(f => f.SiteId == siteId)
                .OrderByDescending(f => f.Version)
                .Select(f => new BookingFormDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Version = f.Version,
                    Active = f.Active,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync(ct);
        }

        public async Task<Guid> CreateFormAsync(Guid siteId, BookingFormCreateRequest req, CancellationToken ct)
        {
            // próxima versión
            var next = await _db.BookingForms.Where(f => f.SiteId == siteId).MaxAsync(f => (int?)f.Version, ct) ?? 0;
            var ver = next + 1;

            if (req.Active)
            {
                var others = await _db.BookingForms.Where(f => f.SiteId == siteId && f.Active).ToListAsync(ct);
                foreach (var o in others) o.Active = false;
            }

            var form = new BookingForm
            {
                Id = Guid.NewGuid(),
                SiteId = siteId,
                Name = req.Name.Trim(),
                Version = ver,
                Active = req.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.BookingForms.Add(form);
            await _db.SaveChangesAsync(ct);
            return form.Id;
        }

        public async Task<Guid> AddFieldAsync(Guid formId, BookingFormFieldCreateRequest req, CancellationToken ct)
        {
            ValidateFieldType(req.Type);

            var form = await _db.BookingForms.FirstOrDefaultAsync(f => f.Id == formId, ct)
                ?? throw new KeyNotFoundException("Formulario no encontrado.");

            // field_name único por form
            var exists = await _db.BookingFormFields.AnyAsync(f => f.FormId == formId && f.FieldName == req.FieldName, ct);
            if (exists) throw new InvalidOperationException("FieldName ya existe en este formulario.");

            var field = new BookingFormField
            {
                Id = Guid.NewGuid(),
                FormId = formId,
                Type = req.Type,
                FieldName = req.FieldName,
                Label = req.Label,
                Required = req.Required,
                HelpText = req.HelpText,
                ValidationRegex = req.ValidationRegex,
                Options = req.Options is null ? null : JsonDocument.Parse(JsonSerializer.Serialize(req.Options)),
                Position = req.Position
            };

            _db.BookingFormFields.Add(field);
            await _db.SaveChangesAsync(ct);
            return field.Id;
        }

        public async Task UpdateFieldAsync(Guid fieldId, BookingFormFieldUpdateRequest req, CancellationToken ct)
        {
            var field = await _db.BookingFormFields.FirstOrDefaultAsync(f => f.Id == fieldId, ct)
                ?? throw new KeyNotFoundException("Campo no encontrado.");

            if (req.Label is not null) field.Label = req.Label;
            if (req.Required.HasValue) field.Required = req.Required.Value;
            if (req.HelpText is not null) field.HelpText = req.HelpText;
            if (req.ValidationRegex is not null) field.ValidationRegex = req.ValidationRegex;
            if (req.Options is not null) field.Options = JsonDocument.Parse(JsonSerializer.Serialize(req.Options));
            if (req.Position.HasValue) field.Position = req.Position.Value;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteFieldAsync(Guid fieldId, CancellationToken ct)
        {
            var field = await _db.BookingFormFields.FirstOrDefaultAsync(f => f.Id == fieldId, ct)
                ?? throw new KeyNotFoundException("Campo no encontrado.");

            _db.BookingFormFields.Remove(field);
            await _db.SaveChangesAsync(ct);
        }

        // ===== helpers =====
        private static void ValidateFlowOrder(string flow)
        {
            if (flow is not ("service" or "staff" or "date"))
                throw new InvalidOperationException("DefaultFlowOrder inválido.");
        }

        private static void ValidateStep(string step)
        {
            if (step is not ("service" or "staff" or "date" or "extras" or "form"))
                throw new InvalidOperationException("Step inválido.");
        }

        private static void ValidateFieldType(string type)
        {
            if (type is not ("text" or "textarea" or "number" or "select" or "checkbox" or "date" or "email" or "phone"))
                throw new InvalidOperationException("Tipo de campo inválido.");
        }
    }
}
