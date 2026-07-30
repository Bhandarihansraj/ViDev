/**
 * netcn Type System — Barrel Export
 *
 * Import everything from here:
 *   import type { AstNode, Wire, Template } from '@/types';
 */

export type {
  // AST types
  NodeType,
  HttpVerb,
  ServiceLifetime,
  AnnotationBadge,
  ServiceCallStatement,
  ReturnStatement,
  BodyStatement,
  AstParameter,
  AstMethod,
  PropertyType,
  AstProperty,
  Plug,
  Socket,
  ControllerNode,
  ServiceNode,
  EntityNode,
  AstNode,
  AstEdge,
} from './ast';

export type {
  // Wiring types
  WireTransform,
  WireStatus,
  ArchLayer,
  WireEndpoint,
  Wire,
  WiringContract,
  TypeCompatibilityResult,
} from './wiring';

export type {
  // Template types
  Template,
  TemplateAst,
  TemplateSummary,
  SaveTemplateRequest,
  GenerateRequest,
  GenerateResponse,
} from './template';
