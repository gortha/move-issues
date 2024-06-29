import axios from "axios";
import * as dotenv from 'dotenv';

dotenv.config();

const githubFromToken = process.env.GITHUB_FROM_TOKEN;
const githubToToken = process.env.GITHUB_TO_TOKEN;

const githubFromHeaders = {
    Authorization: `token ${githubFromToken}`,
    Accept: 'application/vnd.github.full+json',
};
const githubToHeaders = {
    Authorization: `token ${githubToToken}`,
    Accept: 'application/vnd.github.full+json',
};

interface GithubIssueLabel {
    name: string;
}

interface GithubIssueAssignee {
    login: string;
}

interface GithubIssueComment {
    body: string;
}

interface GithubIssue {
    id: number;
    title: string;
    body: string;
    assignee: GithubIssueAssignee
    number: number;
    assignees: GithubIssueAssignee[];
    labels: GithubIssueLabel[];
    state: string,
    state_reason: string,
    comments: number,
}

const getIssues = async (repoUrl: string) => {
    let allIssues: GithubIssue[] = [];
    let page = 1;
    const perPage = 100;
    let queryString = `state=all&per_page=${perPage}`;

    try {
        while (true) {
            const response = await axios.get(
                `https://api.github.com/repos/${repoUrl}/issues?${queryString}&page=${page}`,
                { headers: githubFromHeaders }
            );

            allIssues = [...allIssues, ...response.data];

            if (response.data.length < perPage) {
                break;
            }

            page++;
        }
        console.log(`GetIssues in repo ${repoUrl}: ${JSON.stringify(allIssues.map(i => i.title))}`);
        return allIssues;
    } catch (error: any) {
        if (error?.response?.status === 404) {
            console.warn(`Not found while getting issues`);
            return [];
        }
        else {
            console.error(`Error getting issues`);
            handleError(error);
            process.exit(1);
        }
    }
};

const getIssuesComments = async (repoUrl: string, issueNumber: number) => {
    let allIssueComments: GithubIssueComment[] = [];
    let page = 1;
    const perPage = 100;
    let queryString = `per_page=${perPage}`;

    try {
        while (true) {
            const response = await axios.get(
                `https://api.github.com/repos/${repoUrl}/issues/${issueNumber}/comments?${queryString}&page=${page}`,
                { headers: githubFromHeaders }
            );

            allIssueComments = [...allIssueComments, ...response.data];

            if (response.data.length < perPage) {
                break;
            }

            page++;
        }

        console.log(`GetIssueComments in repo ${repoUrl}: ${JSON.stringify(allIssueComments.map(i => i.body))}`);
        return allIssueComments;
    } catch (error: any) {
        if (error?.response?.status === 404) {
            console.warn(`Not found while getting issue ${issueNumber} comments`);
            return [];
        }
        else {
            console.error(`Error getting issue ${issueNumber} comments`);
            handleError(error);
            process.exit(1);
        }
    }
};

const createIssueTarget = async (title: string, body: string | null, labels: string[], assignees: string[]) => {
    try {
        const response = await axios.post(
            `https://api.github.com/repos/${process.env.GITHUB_TO_REPO_URL}/issues`,
            { title, body, labels, assignees },
            { headers: githubToHeaders }
        );
        console.log('Created issue:', response.data.url);
        return response.data.number;
    } catch (error) {
        console.error('Error creating issue:');
        handleError(error);
        process.exit(1);
    }
};

const patchIssueTarget = async (issueNumber: number, state: string, state_reason: string) => {
    try {
        const response = await axios.patch(
            `https://api.github.com/repos/${process.env.GITHUB_TO_REPO_URL}/issues/${issueNumber}`,
            { state, state_reason },
            { headers: githubToHeaders }
        );
        console.log('Patched issue:', response.data.url);
    } catch (error) {
        console.error('Error patching issue:');
        handleError(error);
        process.exit(1);
    }
};

const createIssueCommentTarget = async (issueNumber: number, body: string | null) => {
    try {
        const response = await axios.post(
            `https://api.github.com/repos/${process.env.GITHUB_TO_REPO_URL}/issues/${issueNumber}/comments`,
            { body },
            { headers: githubToHeaders }
        );
        console.log('Created issue comment:', response.data.url);
    } catch (error) {
        console.error('Error creating issue comment');
        handleError(error);
        process.exit(1);
    }
};

const transferIssues = async () => {
    console.info
    if (!process.env.GITHUB_FROM_REPO_URL || !process.env.GITHUB_TO_REPO_URL) {
        console.error('Source or target repository is not specified.');
        process.exit(1);
    }

    const issuesFrom = (await getIssues(process.env.GITHUB_FROM_REPO_URL)) || [];
    const issuesTo = (await getIssues(process.env.GITHUB_TO_REPO_URL)) || [];
    const issuesDiff = issuesFrom.filter(i => !issuesTo.map(it => it.title).includes(i.title));

    console.info(`Number issue(s) to transfer: ${issuesDiff.length}`);
    for (const issue of issuesDiff) {
        const labelNames = issue.labels.map(label => label.name);
        const assigneeLogins = issue.assignees.map(assignee => assignee.login);

        console.log(`Creating issue: ${issue.title} in ${process.env.GITHUB_TO_REPO_URL}`);
        const issueCreatedNumber = await createIssueTarget(issue.title, issue.body, labelNames, assigneeLogins);
        await sleep(1000);

        // Patch not open state
        if (issue.state !== 'open') {
            console.log(`Patch issue ${issue.title} - ${issue.title} from open to ${issue.state} - in ${process.env.GITHUB_TO_REPO_URL}`);
            await patchIssueTarget(issueCreatedNumber, issue.state, issue.state_reason);
        }

        // Add Comments
        if (issue.comments > 0) {
            const commentsFrom = (await getIssuesComments(process.env.GITHUB_FROM_REPO_URL, issue.number)) || [];            
            console.info(`Number issue: ${issue.title} - comments to transfer: ${commentsFrom.length}`);
            for (const comment of commentsFrom) {
                console.log(`Create issue: ${issue.title} - comment: ${comment.body} in ${process.env.GITHUB_TO_REPO_URL}`);
                await createIssueCommentTarget(issueCreatedNumber, comment.body || '');
                await sleep(1000);
            }
        }
        await sleep(1000);
    }
};

const handleError = (error: any) => {
    if (error.response) {
        if (error?.response?.status === 404) {
            console.warn(`W Error: ${error.message}`);
            console.warn(`Status: ${error.response.status}`);
            console.warn(`Data: ${error.response.data.message}`);
        }
        else {
            console.error(`Error: ${error.message}`);
            console.error(`Status: ${error.response.status}`);
            console.error(`Data: ${error.response.data.message}`);
        }
    } else if (error.request) {
        console.error(`Error: ${error.message}`);
        console.error('The request was made but no response was received');
    } else {
        console.error('Error:', error.message);
    }
    process.exit(1);
};

const sleep = (ms: number) => {
    return new Promise(resolve => setTimeout(resolve, ms));
}

transferIssues();