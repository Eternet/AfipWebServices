# Clientes de servicios de ARCA

`AfipWebServicesClient` contiene autenticación WSAA y clientes SOAP. Requiere .NET 9.

## Constancia de inscripción

`TaxRegistrationClient` consulta `getPersona_v2` de `ws_sr_constancia_inscripcion`, con `LoginCmsClient` y un ticket específico para el padrón. Selecciona las direcciones oficiales de producción u homologación según `AfipEnvironment`. El certificado debe tener asociado ese servicio.

El adaptador lee el XML vigente directamente, incluyendo `impuesto/estadoImpuesto`, en lugar de usar el proxy A5 generado en 2024, que no representa esos campos. El proxy anterior se conserva por compatibilidad con sus consumidores. Referencia: [manual oficial WSCI 4.1](https://www.arca.gob.ar/ws/WSCI/manual_ws_sr_ws_constancia_inscripcion.pdf).

La interpretación inicial reconoce exactamente una inscripción activa (`AC`): IVA (30 → RI), monotributo (20 → MT) o IVA exento (32 → EX). Identidad distinta, clave inactiva, respuesta parcial, inscripciones incompatibles y ausencia de condición verificable generan `TaxRegistrationException` con código explícito. No se infiere consumidor final a partir de una respuesta vacía. Contabilidad debe validar las reglas con casos reales antes de su uso operativo.

`OriginalResponse` conserva únicamente `personaReturn`, sin el pedido SOAP ni las credenciales WSAA. Nunca registrar token, firma, contraseña o certificado. La caché existente de tickets WSAA está excluida de Git y debe quedar en un directorio del servicio con acceso restringido.

El endpoint autenticado y la persistencia de consultas se implementan en `Eternet.Invoices.Out` (`TaxRegistrations`); la integración de usuario vive en `dashboard-Administracion-Finanzas/liquidador-iva`. Seguimiento: [issue #2](https://github.com/Eternet/AfipWebServices/issues/2).
