import { NextRequest, NextResponse } from 'next/server'

// Helper function to verify authentication
function verifyAuth(request: NextRequest): { valid: boolean; error?: string } {
    const authHeader = request.headers.get('authorization')

    if (!authHeader) {
        return { valid: false, error: 'Unauthorized' }
    }

    if (!authHeader.startsWith('Bearer ')) {
        return { valid: false, error: 'Invalid authorization format' }
    }

    const token = authHeader.split(' ')[1]
    if (!token) {
        return { valid: false, error: 'No token provided' }
    }

    return { valid: true }
}

// GET - List all downloads
export async function GET(request: NextRequest) {
    const authCheck = verifyAuth(request)
    if (!authCheck.valid) {
        return NextResponse.json(
            { error: authCheck.error },
            { status: 401 }
        )
    }

    try {
        const bucketName = 'my-bucket'
        const authHeader = request.headers.get('authorization')

        const listResponse = await fetch(
            `http://localhost:5000/api/file/list/${bucketName}`,
            {
                method: 'GET',
                headers: {
                    'Authorization': authHeader || '',
                    'Content-Type': 'application/json',
                },
                cache: 'no-store',
            }
        )

        if (!listResponse.ok) {
            if (listResponse.status === 401) {
                return NextResponse.json(
                    { error: 'Unauthorized' },
                    { status: 401 }
                )
            }
            throw new Error(`MinIO list error: ${listResponse.status}`)
        }

        const files: string[] = await listResponse.json()

        const downloads = files.map((fileName) => {
            const lastDotIndex = fileName.lastIndexOf('.')
            const extension = lastDotIndex !== -1
                ? fileName.slice(lastDotIndex + 1).toLowerCase()
                : 'unknown'

            const nameWithoutExt = lastDotIndex !== -1
                ? fileName.slice(0, lastDotIndex)
                : fileName

            const lastOpenBracket = nameWithoutExt.lastIndexOf('[')
            const lastCloseBracket = nameWithoutExt.lastIndexOf(']')

            const quality = (lastOpenBracket !== -1 && lastCloseBracket !== -1 && lastCloseBracket > lastOpenBracket)
                ? nameWithoutExt.slice(lastOpenBracket + 1, lastCloseBracket).trim()
                : 'Unknown'

            const titleWithAuthor = lastOpenBracket !== -1
                ? nameWithoutExt.slice(0, lastOpenBracket).trim()
                : nameWithoutExt.trim()

            return {
                id: fileName,
                title: titleWithAuthor,
                author: 'Unknown',
                format: extension.toUpperCase(),
                size: 'N/A',
                date: new Date().toISOString().split('T')[0],
                downloads: 0,
                fileName: fileName,
                quality: quality,
            }
        })

        return NextResponse.json(downloads, { status: 200 })
    } catch (error) {
        return NextResponse.json(
            { error: 'Failed to fetch downloads' },
            { status: 500 }
        )
    }
}

// DELETE - Delete a file from MinIO
export async function DELETE(request: NextRequest) {
    const authCheck = verifyAuth(request)
    if (!authCheck.valid) {
        return NextResponse.json(
            { error: authCheck.error },
            { status: 401 }
        )
    }

    try {
        const { searchParams } = new URL(request.url)
        const fileName = searchParams.get('fileName')
        const bucketName = 'my-bucket'
        const authHeader = request.headers.get('authorization')

        if (!fileName) {
            return NextResponse.json(
                { error: 'fileName is required' },
                { status: 400 }
            )
        }

        const deleteUrl = `http://localhost:5000/api/file/delete/${bucketName}/${encodeURIComponent(fileName)}`

        const deleteResponse = await fetch(deleteUrl, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authHeader || '',
            },
        })

        if (!deleteResponse.ok) {
            if (deleteResponse.status === 401) {
                return NextResponse.json(
                    { error: 'Unauthorized' },
                    { status: 401 }
                )
            }

            const errorData = await deleteResponse.json().catch(() => ({}))
            throw new Error(errorData.error || `Delete failed with status ${deleteResponse.status}`)
        }

        // Success
        return NextResponse.json(
            { success: true, message: `File "${fileName}" deleted successfully` },
            { status: 200 }
        )
    } catch (error) {
        return NextResponse.json(
            { error: error instanceof Error ? error.message : 'Failed to delete file' },
            { status: 500 }
        )
    }
}
