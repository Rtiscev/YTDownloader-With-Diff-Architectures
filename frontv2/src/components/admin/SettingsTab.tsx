// app/admin/components/SettingsTab.tsx
'use client'

import React from 'react'

export default function SettingsTab() {
    return (
        <div className="space-y-6">
            <div className="p-6 rounded-2xl bg-white/5 border border-white/10">
                <h2 className="text-xl font-bold mb-4">General Settings</h2>
                <div className="space-y-4">
                    <div>
                        <label className="block text-sm font-semibold text-gray-300 mb-2">Application Name</label>
                        <input
                            type="text"
                            defaultValue="YouTubeDown"
                            className="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-lg focus:border-cyan-500 focus:outline-none text-white"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-gray-300 mb-2">Max Download Size (GB)</label>
                        <input
                            type="number"
                            defaultValue="1"
                            className="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-lg focus:border-cyan-500 focus:outline-none text-white"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-gray-300 mb-2">Enable Notifications</label>
                        <input type="checkbox" defaultChecked className="w-4 h-4" />
                    </div>
                    <button className="w-full mt-4 py-2 bg-gradient-to-r from-blue-500 to-cyan-500 rounded-lg font-semibold hover:shadow-lg hover:shadow-cyan-500/50 transition-all">
                        Save Settings
                    </button>
                </div>
            </div>
        </div>
    )
}
