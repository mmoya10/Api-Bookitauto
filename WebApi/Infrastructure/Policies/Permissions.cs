namespace WebApi.Infrastructure.Policies
{
    public static class Permissions
    {
        // ========== CASH ==========
        public static class Cash
        {
            public const string View = "cash:view";         // Ver caja / sesiones / movimientos
            public const string Manage = "cash:manage";     // Abrir/cerrar sesión, crear/eliminar movimientos
        }

        // ========== PRODUCTOS ==========
        public static class Products
        {
            public const string View = "products:view";     // Ver productos
            public const string Manage = "products:manage"; // Crear/editar/eliminar productos
        }

        // ========== STOCK ==========
        public static class Stock
        {
            public const string View = "stock:view";        // Ver stock y movimientos
            public const string Manage = "stock:manage";    // Crear/eliminar movimientos
        }

        // ========== ESPACIOS/RECURSOS ==========
        public static class Resources
        {
            public const string View = "resources:view";     // Ver recursos/espacios/equipos
            public const string Manage = "resources:manage"; // CRUD de recursos
        }

        // ========== CALENDARIOS ==========
        // Nota: si NO tiene Calendars.View → solo puede ver SUS citas (lo aplicas en queries).
        public static class Calendars
        {
            public const string View = "calendars:view";       // Ver calendario completo de la sucursal
            public const string Manage = "calendars:manage";   // Cambiar vista/ajustes del calendario, reglas, etc.
        }

        // ========== BOOKINGS (CITAS) ==========
        public static class Bookings
        {
            public const string View = "bookings:view";       // Listar búsquedas/consultas (con Calendars.View verá todo, si no, solo propias)
            public const string Manage = "bookings:manage";    // Crear/editar/cancelar citas
        }

        // (Opcional) Usuarios, si lo quieres mantener:
        public static class Users
        {
            public const string View = "users:view";
            public const string Manage = "users:manage";
        }

        // REGLAS GENERALES PARA STAFF:
        // - Staff NO tiene permisos sobre negocio/sucursales/facturación.
        // - Staff puede proponer cambios de su horario (no directo): gestionarás la aprobación con lógica, no con permiso.
    }
}
