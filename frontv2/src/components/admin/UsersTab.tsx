'use client'

import React, { useState, useEffect } from 'react'
import { Trash2, Search, RefreshCw, Shield } from 'lucide-react'

interface User {
    id: string
    firstName: string
    lastName: string
    email: string
    roles: string[]
    createdAt: string
}

export default function UsersTab() {
    const [users, setUsers] = useState<User[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)
    const [searchQuery, setSearchQuery] = useState('')
    const [deletingId, setDeletingId] = useState<string | null>(null)

    useEffect(() => {
        fetchUsers()
    }, [])

    const fetchUsers = async () => {
        try {
            console.log('[UsersTab] Starting fetch...')
            setLoading(true)
            setError(null)

            // ✓ Get token from localStorage
            const token = localStorage.getItem('accessToken')
            if (!token) {
                console.error('[UsersTab] ✗ No token found')
                setError('Authentication required. Please login.')
                setLoading(false)
                return
            }

            console.log('[UsersTab] Fetching users with authentication...')

            // ✓ Pass Authorization header
            const response = await fetch('/api/admin/users', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`,
                },
            })

            console.log('[UsersTab] Response status:', response.status)

            // ✓ Handle 401 Unauthorized
            if (response.status === 401) {
                console.error('[UsersTab] ✗ Unauthorized (401): Token invalid or expired')
                localStorage.removeItem('accessToken')
                setError('Session expired. Please login again.')
                setLoading(false)
                return
            }

            // ✓ Handle 403 Forbidden
            if (response.status === 403) {
                console.error('[UsersTab] ✗ Forbidden (403): Admin role required')
                setError('You do not have permission to access users.')
                setLoading(false)
                return
            }

            if (!response.ok) {
                throw new Error(`Failed to fetch users (${response.status})`)
            }

            const data = await response.json()
            console.log('[UsersTab] ✓ Users loaded:', data.length, 'records')
            setUsers(data)
        } catch (err) {
            console.error('[UsersTab] Fetch error:', err)
            setError(err instanceof Error ? err.message : 'Failed to load users')
        } finally {
            setLoading(false)
        }
    }

    const handleDeleteUser = async (userId: string, fullName: string) => {
        if (!confirm(`Are you sure you want to delete ${fullName}? This action cannot be undone.`)) {
            return
        }

        try {
            console.log('[UsersTab] Deleting user:', userId)
            setDeletingId(userId)

            // ✓ Get token from localStorage
            const token = localStorage.getItem('accessToken')
            if (!token) {
                console.error('[UsersTab] ✗ No token found for delete')
                alert('Authentication required. Please login.')
                setDeletingId(null)
                return
            }

            console.log('[UsersTab] Sending delete request with authentication...')

            // ✓ Pass Authorization header and token
            const response = await fetch(
                `/api/admin/users?userId=${encodeURIComponent(userId)}`,
                {
                    method: 'DELETE',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${token}`,
                    },
                }
            )

            console.log('[UsersTab] Delete response status:', response.status)

            // ✓ Handle 401 Unauthorized
            if (response.status === 401) {
                console.error('[UsersTab] ✗ Unauthorized (401): Token invalid or expired')
                localStorage.removeItem('accessToken')
                alert('Session expired. Please login again.')
                setDeletingId(null)
                return
            }

            // ✓ Handle 403 Forbidden
            if (response.status === 403) {
                console.error('[UsersTab] ✗ Forbidden (403): Admin role required')
                alert('You do not have permission to delete users.')
                setDeletingId(null)
                return
            }

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}))
                throw new Error(errorData.error || `Failed to delete user (${response.status})`)
            }

            console.log('[UsersTab] ✓ User deleted successfully')
            setUsers(prev => prev.filter(u => u.id !== userId))
            alert('User deleted successfully!')
        } catch (err) {
            console.error('[UsersTab] Delete error:', err)
            alert(err instanceof Error ? err.message : 'Failed to delete user')
        } finally {
            setDeletingId(null)
        }
    }

    const filteredUsers = users.filter(u => {
        const query = searchQuery.toLowerCase()
        const firstName = u.firstName?.toLowerCase() || ''
        const lastName = u.lastName?.toLowerCase() || ''
        const email = u.email?.toLowerCase() || ''

        return firstName.includes(query) || lastName.includes(query) || email.includes(query)
    })

    if (loading) {
        return (
            <div className="flex items-center justify-center p-12">
                <div className="text-center">
                    <RefreshCw className="w-8 h-8 animate-spin mx-auto mb-2 text-cyan-400" />
                    <p className="text-gray-400">Loading users...</p>
                </div>
            </div>
        )
    }

    if (error) {
        return (
            <div className="p-6 rounded-2xl bg-red-500/10 border border-red-500/20">
                <p className="text-red-400">Error: {error}</p>
                <button
                    onClick={fetchUsers}
                    className="mt-4 px-4 py-2 bg-red-500/20 rounded-lg hover:bg-red-500/30 transition-all"
                >
                    Retry
                </button>
            </div>
        )
    }

    return (
        <div className="space-y-4">
            <div className="flex items-center gap-4">
                <div className="flex-1 relative">
                    <Search className="absolute left-3 top-3 w-5 h-5 text-gray-500" />
                    <input
                        type="text"
                        placeholder="Search users..."
                        value={searchQuery}
                        onChange={e => setSearchQuery(e.target.value)}
                        className="w-full pl-10 pr-4 py-2 bg-white/5 border border-white/10 rounded-lg focus:border-cyan-500 focus:outline-none transition-all text-white"
                    />
                </div>
                <button
                    onClick={fetchUsers}
                    disabled={loading}
                    className="flex items-center gap-2 px-4 py-2 bg-white/10 rounded-lg hover:bg-white/20 transition-all disabled:opacity-50"
                >
                    <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
                    Refresh
                </button>
            </div>

            <div className="text-sm text-gray-400">
                Showing {filteredUsers.length} of {users.length} users
            </div>

            <div className="rounded-2xl overflow-hidden border border-white/10 bg-white/5">
                <table className="w-full">
                    <thead>
                        <tr className="border-b border-white/10">
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">First Name</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Last Name</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Email</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Roles</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Created</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {filteredUsers.length === 0 ? (
                            <tr>
                                <td colSpan={6} className="px-6 py-8 text-center text-gray-400">
                                    {users.length === 0 ? 'No users found' : 'No users match your search'}
                                </td>
                            </tr>
                        ) : (
                            filteredUsers.map(user => (
                                <tr key={user.id} className="border-b border-white/10 hover:bg-white/5 transition-all">
                                    <td className="px-6 py-4 text-sm">
                                        <div className="flex items-center gap-2">
                                            <Shield className="w-4 h-4 text-blue-400" />
                                            <span className="font-semibold">{user.firstName || 'N/A'}</span>
                                        </div>
                                    </td>
                                    <td className="px-6 py-4 text-sm">
                                        <span className="font-semibold">{user.lastName || 'N/A'}</span>
                                    </td>
                                    <td className="px-6 py-4 text-sm text-gray-400">
                                        {user.email || 'N/A'}
                                    </td>
                                    <td className="px-6 py-4 text-sm text-gray-400">
                                        {user.roles?.join(', ') || '—'}
                                    </td>
                                    <td className="px-6 py-4 text-sm text-gray-400">
                                        {new Date(user.createdAt).toLocaleDateString()}
                                    </td>
                                    <td className="px-6 py-4 text-sm">
                                        <button
                                            onClick={() => handleDeleteUser(
                                                user.id,
                                                `${user.firstName} ${user.lastName}`.trim() || 'this user'
                                            )}
                                            disabled={deletingId === user.id}
                                            className="p-1.5 hover:bg-red-500/10 rounded transition-all disabled:opacity-50 disabled:cursor-not-allowed group"
                                            title="Delete user"
                                        >
                                            {deletingId === user.id ? (
                                                <RefreshCw className="w-4 h-4 text-red-400 animate-spin" />
                                            ) : (
                                                <Trash2 className="w-4 h-4 text-red-400 group-hover:text-red-300" />
                                            )}
                                        </button>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    )
}
