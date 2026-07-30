/**
 * netcn Template Metadata — TypeScript Definitions
 * Version: 1.0
 * Source of truth: TRD.md §3.3, PRD.md FR5
 *
 * A Template is a saved architecture design (AST + wiring contract).
 * Templates are published to the Template Hub, where other users can
 * browse, fork, and generate runnable projects from them.
 */

import type { AstNode, AstEdge } from './ast';
import type { WiringContract } from './wiring';

// ---------------------------------------------------------------------------
// Template (the publishable unit)
// ---------------------------------------------------------------------------

/**
 * A complete template — the full saveable/publishable unit.
 * This is what gets POSTed to /templates and stored in the DB.
 *
 * The `ast` field holds the architecture graph (nodes + edges).
 * The `wiring` field holds the contract layer (field-level connections).
 * Together they are the source of truth for code generation.
 */
export interface Template {
  /** Unique template ID (UUID, assigned by backend on save). */
  id: string;
  /** Template name in namespace format (e.g. "bhandarihansraj/clean-auth"). */
  name: string;
  /** Author's user ID. */
  authorId: string;
  /** Author's display name. */
  authorName: string;
  /** Human-readable description of what this template produces. */
  description: string;
  /** Searchable tags (e.g. ["jwt", "minimal-api", "net8"]). */
  tags: string[];
  /** Semantic version string (e.g. "1.0.0"). */
  version: string;
  /** The architecture graph. */
  ast: TemplateAst;
  /** The wiring contract (field-level connections between layers). */
  wiring: WiringContract;
  /** Total download count (read-only, updated by backend). */
  downloadCount: number;
  /** Whether this template passed the compile-check gate. */
  isVerified: boolean;
  /** ISO 8601 timestamp of creation. */
  createdAt: string;
  /** ISO 8601 timestamp of last update. */
  updatedAt: string;
}

/**
 * The AST portion of a template — nodes and their visual connections.
 * This is the JSON that the canvas serializes/deserializes.
 */
export interface TemplateAst {
  /** All architectural nodes on the canvas. */
  nodes: AstNode[];
  /** Visual edges (dependency arrows) between nodes. */
  edges: AstEdge[];
}

// ---------------------------------------------------------------------------
// Template Hub DTOs (what the API returns)
// ---------------------------------------------------------------------------

/** Lightweight template info for browse/search results (no full AST). */
export interface TemplateSummary {
  id: string;
  name: string;
  authorName: string;
  description: string;
  tags: string[];
  version: string;
  downloadCount: number;
  isVerified: boolean;
  createdAt: string;
}

/** Request body for creating/updating a template. */
export interface SaveTemplateRequest {
  name: string;
  description: string;
  tags: string[];
  ast: TemplateAst;
  wiring: WiringContract;
}

/** Request body for the /generate endpoint. */
export interface GenerateRequest {
  /** The AST to generate code from. */
  ast: TemplateAst;
  /** The wiring contract to generate mappers from. */
  wiring: WiringContract;
}

/** Response from the /generate endpoint. */
export interface GenerateResponse {
  /** Generation job ID (used to poll status). */
  jobId: string;
  /** Current job status. */
  status: 'queued' | 'compiling' | 'success' | 'failed';
  /** Download URL for the ZIP (only present when status = 'success'). */
  outputUrl?: string;
  /** Error message (only present when status = 'failed'). */
  error?: string;
}
