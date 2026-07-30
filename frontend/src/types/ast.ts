/**
 * netcn AST Schema — TypeScript Definitions
 * Version: 1.0
 * Source of truth: TRD.md §3.1
 *
 * This file defines the canonical shape of an architecture design.
 * The canvas produces these types. The backend consumes them.
 * Roslyn reads them to generate real C# code.
 *
 * RULE: Every field here must map 1:1 to a field in schema/ast.schema.json.
 */

// ---------------------------------------------------------------------------
// Node Types
// ---------------------------------------------------------------------------

/** The three architectural building blocks a user can place on the canvas. */
export type NodeType = 'Controller' | 'Service' | 'Entity';

/** HTTP verbs supported on controller methods. */
export type HttpVerb = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';

/** DI lifetime scopes for services (maps to AddScoped/AddTransient/AddSingleton). */
export type ServiceLifetime = 'Scoped' | 'Transient' | 'Singleton';

/**
 * Allowed annotation badges.
 * MVP ships with 3: ApiController, Authorize, JWT.
 * This is a FIXED allow-list per SECURITY.md §4 — never let AST content
 * specify arbitrary annotations.
 */
export type AnnotationBadge =
  | 'ApiController'
  | 'Authorize'
  | 'AllowAnonymous'
  | 'JWT'
  | 'Route'
  | 'ValidateModel';

// ---------------------------------------------------------------------------
// Method Body Statements (what a method "does")
// ---------------------------------------------------------------------------

/** A call to an injected service method. */
export interface ServiceCallStatement {
  type: 'ServiceCall';
  /** ID of the Service node being called. */
  service: string;
  /** Method name on that service. */
  method: string;
}

/** A return statement. */
export interface ReturnStatement {
  type: 'Return';
  /** Human-readable description of what is returned (e.g. "JWT token"). */
  value: string;
}

/** Union of all possible method body statement types. */
export type BodyStatement = ServiceCallStatement | ReturnStatement;

// ---------------------------------------------------------------------------
// Method & Parameter shapes
// ---------------------------------------------------------------------------

/** A parameter on a controller or service method. */
export interface AstParameter {
  name: string;
  /** C# type name, e.g. "LoginDto", "int", "string". */
  type: string;
  /** If true, parameter comes from request body ([FromBody]). */
  fromBody?: boolean;
  /** If true, parameter comes from route ([FromRoute]). */
  fromRoute?: boolean;
  /** If true, parameter comes from query string ([FromQuery]). */
  fromQuery?: boolean;
}

/** A method on a Controller or Service node. */
export interface AstMethod {
  name: string;
  /** HTTP verb — only present on Controller methods. */
  verb?: HttpVerb;
  /** Route segment — only present on Controller methods (e.g. "login", "{id}"). */
  route?: string;
  /** Annotations applied to this specific method. */
  annotations: AnnotationBadge[];
  /** Method parameters. */
  parameters: AstParameter[];
  /** C# return type (e.g. "IActionResult", "Task<UserDto>", "void"). */
  returnType: string;
  /**
   * Ordered list of body statements.
   * The code generator walks this list to produce the method body.
   */
  body: BodyStatement[];
}

// ---------------------------------------------------------------------------
// Entity Properties (DB columns)
// ---------------------------------------------------------------------------

/** C# types commonly used for entity properties. */
export type PropertyType =
  | 'int'
  | 'long'
  | 'string'
  | 'bool'
  | 'DateTime'
  | 'decimal'
  | 'double'
  | 'Guid';

/** A property (column) on an Entity node. */
export interface AstProperty {
  name: string;
  type: PropertyType | string;
  /** If true, this property is the primary key. */
  isPrimaryKey?: boolean;
  /** If true, this column is required (NOT NULL). */
  isRequired?: boolean;
  /** Max length constraint (for string columns). */
  maxLength?: number;
}

// ---------------------------------------------------------------------------
// Plugs & Sockets (the wiring board's connection points)
// ---------------------------------------------------------------------------

/**
 * A Plug is an OUTPUT — data this node exposes to other nodes.
 * Example: A Controller method's return type is a plug.
 */
export interface Plug {
  id: string;
  /** Human-readable label (e.g. "Login → JWT token"). */
  label: string;
  /** The C# type being exposed. */
  dataType: string;
  /** Which method or property this plug belongs to. */
  sourceField: string;
}

/**
 * A Socket is an INPUT — data this node requires from another node.
 * Example: A Controller that calls AuthService.Validate() has a socket
 * requiring IAuthService.
 */
export interface Socket {
  id: string;
  /** Human-readable label (e.g. "Needs IAuthService"). */
  label: string;
  /** The C# type required. */
  dataType: string;
  /** Which parameter or dependency this socket satisfies. */
  targetField: string;
}

// ---------------------------------------------------------------------------
// Node Definitions (the 3 architectural building blocks)
// ---------------------------------------------------------------------------

/** Fields shared by every node type. */
interface BaseNode {
  /** Unique identifier for this node (e.g. "AuthController"). */
  id: string;
  /** Discriminator — which type of architectural block this is. */
  type: NodeType;
  /** Display name shown on the canvas. */
  name: string;
  /** Annotation badges applied to this node. */
  annotations: AnnotationBadge[];
  /** Canvas position — where the node sits visually. */
  position: { x: number; y: number };
  /** Output connection points. */
  plugs: Plug[];
  /** Input connection points. */
  sockets: Socket[];
}

/** An API Controller node (e.g. AuthController, UsersController). */
export interface ControllerNode extends BaseNode {
  type: 'Controller';
  /** Route prefix for this controller (e.g. "api/[controller]"). */
  routePrefix: string;
  /** HTTP methods exposed by this controller. */
  methods: AstMethod[];
}

/** A Service node (e.g. AuthService, UserService). */
export interface ServiceNode extends BaseNode {
  type: 'Service';
  /** The interface this service implements (e.g. "IAuthService"). */
  implements: string;
  /** DI lifetime scope. */
  lifetime: ServiceLifetime;
  /** Methods on this service. */
  methods: AstMethod[];
}

/** An Entity node (e.g. User, Order — maps to a DB table). */
export interface EntityNode extends BaseNode {
  type: 'Entity';
  /** DB table name (defaults to pluralized node name). */
  tableName: string;
  /** Columns / properties on this entity. */
  properties: AstProperty[];
}

/** Union of all concrete node types. */
export type AstNode = ControllerNode | ServiceNode | EntityNode;

// ---------------------------------------------------------------------------
// Edges (visual connections on the canvas, NOT wires)
// ---------------------------------------------------------------------------

/**
 * A canvas edge — the visual line between two nodes on React Flow.
 * This represents a dependency (e.g. Controller → Service).
 * NOT the same as a Wire (which is a field-level contract in the wiring board).
 */
export interface AstEdge {
  id: string;
  /** ID of the source node. */
  source: string;
  /** ID of the source handle (plug). */
  sourceHandle?: string;
  /** ID of the target node. */
  target: string;
  /** ID of the target handle (socket). */
  targetHandle?: string;
  /** Optional label shown on the edge. */
  label?: string;
}
