'use client'

import React, { useState } from 'react'
import { useRouter } from 'next/navigation'

export default function Login() {
    const router = useRouter()
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [message, setMessage] = useState('')
    const [loading, setLoading] = useState(false)

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setLoading(true)
        setMessage('')

        try {
            const res = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password }),
            })

            const data = await res.json()

            if (data.success) {
                localStorage.setItem('accessToken', data.accessToken)
                setMessage('Login successful!')
                setTimeout(() => router.push('/'), 1500)
            } else {
                setMessage(data.message || 'Login failed')
            }
        } catch {
            setMessage('Network/Server error')
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-900 to-slate-800 flex items-center justify-center p-4">
            <form
                onSubmit={handleSubmit}
                className="bg-slate-800 p-8 rounded-xl shadow-2xl w-full max-w-sm space-y-4"
            >
                <a
                    href="/"
                    className="block bg-slate-900 text-cyan-400 font-semibold text-sm py-2.5 rounded-lg text-center hover:bg-slate-950 transition-colors"
                >
                    ← Home
                </a>

                <h2 className="text-white text-center text-2xl font-bold tracking-wider">
                    Login
                </h2>

                <input
                    type="email"
                    placeholder="Email"
                    required
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    className="w-full px-4 py-3 rounded-lg border border-slate-700 bg-slate-900 text-white focus:outline-none focus:border-cyan-400 transition-colors"
                />

                <input
                    type="password"
                    placeholder="Password"
                    required
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    className="w-full px-4 py-3 rounded-lg border border-slate-700 bg-slate-900 text-white focus:outline-none focus:border-cyan-400 transition-colors"
                />

                <button
                    type="submit"
                    disabled={loading}
                    className="w-full bg-red-500 hover:bg-red-600 disabled:opacity-50 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-lg transition-colors shadow-lg shadow-red-500/30"
                >
                    {loading ? 'Logging in...' : 'Login'}
                </button>

                {message && (
                    <div
                        className={`p-3 rounded-lg text-center font-medium text-sm ${message.startsWith('Login successful')
                            ? 'bg-green-500/20 text-green-400'
                            : 'bg-red-500/20 text-red-400'
                            }`}
                    >
                        {message}
                    </div>
                )}

                <a
                    href="/signup"
                    className="block text-center text-gray-400 hover:text-gray-300 text-sm mt-4 transition-colors"
                >
                    Don't have an account? Sign up
                </a>
            </form>
        </div>
    )
}
