# Workflow Overview: Hierarchy, Metadata, and Rules Management

This document outlines the workflow for creating a hierarchy, adding approver metadata keys, and associating rules with levels in a hierarchical structure.

## Workflow Steps and Validations

### 1. CreateHierarchyService Flow

#### Validations:
- **Name**:
  - Required.
  - Must be unique.
- **Description**:
  - Maximum length of 500 characters.

#### Database State After Creation:
| **id** | **name**                 | **description**                                | **created_by** | **created_on**         | **last_modified_by** | **last_modified_on** |
|--------|--------------------------|-----------------------------------------------|----------------|-------------------------|-----------------------|-----------------------|
| 1      | Manufacturing Approvals  | Manufacturing department approval workflow    | user123        | 2024-03-14 10:00:00    | user123              | 2024-03-14 10:00:00  |

| **id** | **hierarchy_id** | **key_name** | **created_by** | **created_on**         | **last_modified_by** | **last_modified_on** |
|--------|------------------|--------------|----------------|-------------------------|-----------------------|-----------------------|
| 1      | 1                | level.1      | user123        | 2024-03-14 10:00:00    | user123              | 2024-03-14 10:00:00  |
| 2      | 1                | level.2      | user123        | 2024-03-14 10:00:00    | user123              | 2024-03-14 10:00:00  |

#### Key Points:
- One record in the `hierarchy` table for the hierarchy.
- Two records in the `metadata_key` table represent levels 1 and 2.
- Audit columns (`created_by`, `created_on`, etc.) are populated.

---

### 2. AddApproverMetadataKeyService Flow

#### Validations:
- **HierarchyId**:
  - Must exist in the `hierarchy` table.
- **KeyName**:
  - Required.
  - Must be unique for this hierarchy (prefixed with `ApproverMetadataKey.`).

#### Database State After Adding Metadata Keys:
| **id** | **hierarchy_id** | **key_name**                         | **created_by** | **created_on**         | **last_modified_by** | **last_modified_on** |
|--------|------------------|--------------------------------------|----------------|-------------------------|-----------------------|-----------------------|
| 1      | 1                | level.1                             | user123        | 2024-03-14 10:00:00    | user123              | 2024-03-14 10:00:00  |
| 2      | 1                | level.2                             | user123        | 2024-03-14 10:00:00    | user123              | 2024-03-14 10:00:00  |
| 3      | 1                | ApproverMetadataKey.ExpenseLimit    | user123        | 2024-03-14 10:01:00    | user123              | 2024-03-14 10:01:00  |
| 4      | 1                | ApproverMetadataKey.Department      | user123        | 2024-03-14 10:01:00    | user123              | 2024-03-14 10:01:00  |
| 5      | 1                | ApproverMetadataKey.Region          | user123        | 2024-03-14 10:01:00    | user123              | 2024-03-14 10:01:00  |
| 6      | 1                | ApproverMetadataKey.OverrideLevel   | user123        | 2024-03-14 10:01:00    | user123              | 2024-03-14 10:01:00  |
| 7      | 1                | ApproverMetadataKey.ApprovalType    | user123        | 2024-03-14 10:01:00    | user123              | 2024-03-14 10:01:00  |

---

### 3. AddRuleToLevelService Flow

#### Validations:
- **HierarchyId**:
  - Must exist.
- **LevelNumber**:
  - Must exist for this hierarchy.
- **RuleNumber**:
  - If provided, must be unique for this level.
- **QueryMatrix**:
  - Required.
  - If it contains `ApproverMetadataKey` references, they must exist.

#### Database State After Adding Rules:
| **id** | **hierarchy_id** | **key_name**                                                       | **created_by** | **created_on**         |
|--------|------------------|--------------------------------------------------------------------|----------------|-------------------------|
| 1      | 1                | level.1                                                           | user123        | 2024-03-14 10:00:00    |
| 2      | 1                | level.2                                                           | user123        | 2024-03-14 10:00:00    |
| 3      | 1                | ApproverMetadataKey.ExpenseLimit                                  | user123        | 2024-03-14 10:01:00    |
| 4      | 1                | ApproverMetadataKey.Department                                    | user123        | 2024-03-14 10:01:00    |
| 5      | 1                | ApproverMetadataKey.Region                                        | user123        | 2024-03-14 10:01:00    |
| 6      | 1                | ApproverMetadataKey.OverrideLevel                                 | user123        | 2024-03-14 10:01:00    |
| 7      | 1                | ApproverMetadataKey.ApprovalType                                  | user123        | 2024-03-14 10:01:00    |
| 8      | 1                | level.1.rule.1.query:[_and][ApproverMetadataKey.Department_eq_Manufacturing] | user123        | 2024-03-14 10:02:00    |
| 9      | 1                | level.1.rule.2.query:[_and][ApproverMetadataKey.ExpenseLimit_lte_10000]      | user123        | 2024-03-14 10:02:00    |
| 10     | 1                | level.2.rule.1.query:[_or][ApproverMetadataKey.OverrideLevel_eq_Director]    | user123        | 2024-03-14 10:02:00    |
| 11     | 1                | level.2.rule.2.query:[_or][ApproverMetadataKey.ApprovalType_eq_Emergency]    | user123        | 2024-03-14 10:02:00    |

---

### Summary of Operations
- **Hierarchy Creation**:
  - Single record in `hierarchy` table.
- **Metadata Keys**:
  - Dynamic metadata keys added with `ApproverMetadataKey` prefix.
- **Rules**:
  - Rules linked to levels using the `level.<level_number>.rule.<rule_number>` convention.
- **Validation**:
  - Ensures hierarchy existence, metadata key uniqueness, and query matrix correctness.

---

### Usage
This document helps understand the expected database state after running the workflow, ensuring correctness during testing and debugging.
