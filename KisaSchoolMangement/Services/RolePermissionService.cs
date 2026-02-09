using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using KisaSchoolMangement.Models;
using MySql.Data.MySqlClient;
using System.Windows;

namespace KisaSchoolMangement.Services
{
    public class RolePermissionService
    {
        private readonly string _connectionString;

        public RolePermissionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["KisaSchoolDB"].ConnectionString;
            try
            {
                EnsurePermissionsTablesExist();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database check failed: {ex.Message}", "Database");
            }
        }

        private void EnsurePermissionsTablesExist()
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string[] required = { "roles", "permissions", "role_permissions" };

            foreach (var t in required)
            {
                using var checkCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @name", conn);
                checkCmd.Parameters.AddWithValue("@name", t);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                if (!exists)
                {
                    string? create = t switch
                    {
                        "roles" => @"
CREATE TABLE IF NOT EXISTS `roles` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `name` VARCHAR(100) NOT NULL,
  `description` TEXT NULL,
  `created_at` DATETIME NULL,
  `updated_at` DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
                        "permissions" => @"
CREATE TABLE IF NOT EXISTS `permissions` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `module` VARCHAR(100) NOT NULL,
  `permission_key` VARCHAR(150) NOT NULL UNIQUE,
  `name` VARCHAR(150) NOT NULL,
  `description` TEXT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
                        "role_permissions" => @"
CREATE TABLE IF NOT EXISTS `role_permissions` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `role_id` INT NOT NULL,
  `permission_id` INT NOT NULL,
  `updated_by` INT NULL,
  `updated_at` DATETIME NULL,
  FOREIGN KEY (`role_id`) REFERENCES `roles`(`id`) ON DELETE CASCADE,
  FOREIGN KEY (`permission_id`) REFERENCES `permissions`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
                        _ => null
                    };

                    if (!string.IsNullOrEmpty(create))
                    {
                        using var createCmd = new MySqlCommand(create, conn);
                        createCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static string SafeGetString(MySqlDataReader reader, string columnName)
        {
            int idx = reader.GetOrdinal(columnName);
            return reader.IsDBNull(idx) ? string.Empty : reader.GetString(idx);
        }

        private static string SafeGetDateTimeString(MySqlDataReader reader, string columnName, string format = "yyyy-MM-dd HH:mm")
        {
            int idx = reader.GetOrdinal(columnName);
            return reader.IsDBNull(idx) ? string.Empty : reader.GetDateTime(idx).ToString(format);
        }

        public ObservableCollection<RoleModel> GetAllRoles()
        {
            var roles = new ObservableCollection<RoleModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                string query = "SELECT id, name, description, created_at, updated_at FROM roles ORDER BY name";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    roles.Add(new RoleModel
                    {
                        Id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
                        Name = SafeGetString(reader, "name"),
                        Description = SafeGetString(reader, "description"),
                        CreatedAt = SafeGetDateTimeString(reader, "created_at"),
                        UpdatedAt = SafeGetDateTimeString(reader, "updated_at")
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading roles: {ex.Message}", "Error");
            }

            return roles;
        }

        public ObservableCollection<PermissionModel> GetAllPermissions()
        {
            var permissions = new ObservableCollection<PermissionModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                string query = @"SELECT id, module, permission_key, name, description
                                 FROM permissions
                                 ORDER BY module, name";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    permissions.Add(new PermissionModel
                    {
                        Module = SafeGetString(reader, "module"),
                        Key = SafeGetString(reader, "permission_key"),
                        Name = SafeGetString(reader, "name"),
                        Description = SafeGetString(reader, "description"),
                        IsSelected = false
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading permissions: {ex.Message}", "Error");
            }

            return permissions;
        }

        public HashSet<string> GetPermissionKeysForRole(int roleId)
        {
            var keys = new HashSet<string>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                string query = @"SELECT p.permission_key
                                 FROM role_permissions rp
                                 INNER JOIN permissions p ON rp.permission_id = p.id
                                 WHERE rp.role_id = @RoleId";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@RoleId", roleId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var key = SafeGetString(reader, "permission_key");
                    if (!string.IsNullOrEmpty(key))
                        keys.Add(key);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading role permissions: {ex.Message}", "Error");
            }

            return keys;
        }

        public bool SaveRolePermissions(int roleId, IEnumerable<string> permissionKeys, int updatedBy)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var tx = conn.BeginTransaction();

                using (var deleteCmd = new MySqlCommand("DELETE FROM role_permissions WHERE role_id = @RoleId", conn, tx))
                {
                    deleteCmd.Parameters.AddWithValue("@RoleId", roleId);
                    deleteCmd.ExecuteNonQuery();
                }

                foreach (var key in permissionKeys)
                {
                    string insertQuery = @"INSERT INTO role_permissions (role_id, permission_id, updated_by, updated_at)
                                           SELECT @RoleId, p.id, @UpdatedBy, @UpdatedAt
                                           FROM permissions p
                                           WHERE p.permission_key = @PermissionKey";

                    using var insertCmd = new MySqlCommand(insertQuery, conn, tx);
                    insertCmd.Parameters.AddWithValue("@RoleId", roleId);
                    insertCmd.Parameters.AddWithValue("@PermissionKey", key);
                    insertCmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);
                    insertCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    insertCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving permissions: {ex.Message}", "Error");
                return false;
            }
        }

        public bool CreateRole(string name, string description, int createdBy)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string query = @"INSERT INTO roles (name, description, created_at, updated_at)
                                 VALUES (@Name, @Description, @CreatedAt, @UpdatedAt)";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating role: {ex.Message}", "Error");
                return false;
            }
        }

        public bool CreatePermission(string module, string key, string name, string description)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string query = @"INSERT INTO permissions (module, permission_key, name, description)
                                 VALUES (@Module, @Key, @Name, @Description)";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Module", module);
                cmd.Parameters.AddWithValue("@Key", key);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating permission: {ex.Message}", "Error");
                return false;
            }
        }
    }
}
