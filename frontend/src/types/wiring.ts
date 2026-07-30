/**
 * netcn Wiring Data Model — TypeScript Definitions
 * Version: 1.0
 * Source of truth: TRD.md §3.2, PRD.md FR2
 *
 * The wiring board is the CONTRACT LAYER — it connects fields across
 * UI, API, and DB layers and guarantees name/type consistency.
 * A wire maps 1:1 to a generated mapper line in the output code.
 *
 * SECURITY: The transform allow-list is FIXED and HARDCODED per
 * SECURITY.md §4 and IMPLEMENTATION_PLAN Day 12. Never let AST content
 * specify arbitrary transform code.
 */

// ---------------------------------------------------------------------------
// Transform Types (FIXED allow-list — do NOT add dynamic transforms)
// ---------------------------------------------------------------------------

/**
 * Allowed type transforms between connected fields.
 * Per SECURITY.md §4: this list is backend-defined and fixed.
 * Adding a new transform requires a code change, not user input.
 */
export type WireTransform =
  | 'None'       // Types match exactly, no conversion needed
  | 'ParseInt'   // string → int
  | 'ParseLong'  // string → long
  | 'ParseDate'  // string → DateTime
  | 'ParseGuid'  // string → Guid
  | 'ParseBool'  // string → bool
  | 'ToString';  // any → string

// ---------------------------------------------------------------------------
// Wire Status (visual feedback on the canvas)
// ---------------------------------------------------------------------------

/**
 * The validation state of a wire.
 * - Green:  Types are compatible (exact match or valid transform exists).
 * - Yellow: Types are convertible but require an explicit transform.
 * - Red:    Types are incompatible — generation BLOCKED until resolved.
 */
export type WireStatus = 'green' | 'yellow' | 'red';

// ---------------------------------------------------------------------------
// Wire Endpoint (one side of a connection)
// ---------------------------------------------------------------------------

/** Which architectural layer this endpoint belongs to. */
export type ArchLayer = 'ui' | 'api' | 'db';

/** One side of a wire — identifies a specific field on a specific component. */
export interface WireEndpoint {
  /** The architectural layer (ui, api, db). */
  layer: ArchLayer;
  /** The component/node name (e.g. "LoginForm", "AuthController", "Users"). */
  component: string;
  /** The field name on that component (e.g. "user_id", "UserId"). */
  field: string;
  /** The C# type of this field (e.g. "string", "int", "Guid"). */
  fieldType: string;
}

// ---------------------------------------------------------------------------
// Wire (a single field-level contract connection)
// ---------------------------------------------------------------------------

/**
 * A Wire connects a field on one layer to a field on another layer.
 * Each wire produces exactly one mapper line in the generated code.
 *
 * Example: LoginForm.user_id (string) → AuthController.UserId (int)
 *          transform = ParseInt → generates: dto.UserId = int.Parse(form.user_id);
 */
export interface Wire {
  /** Unique wire identifier. */
  id: string;
  /** Source endpoint (the "from" side). */
  from: WireEndpoint;
  /** Target endpoint (the "to" side). */
  to: WireEndpoint;
  /** The transform applied to convert source type to target type. */
  transform: WireTransform;
  /** Whether this wire is active (included in code generation). */
  isActive: boolean;
  /** Computed validation status (green/yellow/red). */
  status: WireStatus;
}

// ---------------------------------------------------------------------------
// Wiring Contract (the full contract for a project)
// ---------------------------------------------------------------------------

/**
 * The complete wiring contract for a project.
 * This is the data model that the Wiring Board UI renders and edits,
 * and that the code generator reads to produce mapper files.
 *
 * Per PRD FR2: Code generation MUST be blocked while any required
 * wire has status = 'red'.
 */
export interface WiringContract {
  /** The project this wiring contract belongs to. */
  projectId: string;
  /** All wires in this project. */
  wires: Wire[];
}

// ---------------------------------------------------------------------------
// Type Compatibility Check (used by the validation engine)
// ---------------------------------------------------------------------------

/**
 * Result of checking whether two types can be connected.
 * Used by the `CanMap(sourceType, targetType, transform)` function
 * referenced in IMPLEMENTATION_PLAN Day 12.
 */
export interface TypeCompatibilityResult {
  /** Can these types be connected at all? */
  compatible: boolean;
  /** If compatible, which transform is needed? */
  suggestedTransform: WireTransform;
  /** The resulting wire status. */
  status: WireStatus;
  /** Human-readable explanation (shown in Contract Dashboard). */
  message: string;
}
