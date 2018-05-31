clc;clear;close all;

testFiles = dir('/Users/jonas/Documents/SMC /Thesis/PerceptualTest/')
extension = '.csv';

numParticipants = length(testFiles)-6

totals = zeros(numParticipants,3);
means = zeros(numParticipants,3);

speech = zeros(numParticipants,3);
ball = zeros(numParticipants,3);
sb = zeros(numParticipants,2);
sbMeans = zeros(numParticipants,2);

posTotals = zeros(numParticipants,3);
posMeans = zeros(numParticipants,3);

participant = 0;
for k=1:length(testFiles)
   file = testFiles(k).name;

   if(contains(file, extension) == 1)
    participant = participant + 1;   
       
    fid = fopen(file);
    data = textscan(fid,'%s%f%s%f','delimiter',',');
    fclose(fid);
    
    sound = data {1};
    pos = data{2};
    algo = data{3};
    rating = data{4};
        
        %sum the ratings for each algorithm, for each participant
        for i=1:length(data{1})
            if(strcmp('wgw',algo(i)) == 1)
                totals(participant,1) = totals(participant,1) + rating(i);
            end

            if(strcmp('sdn',algo(i)) == 1)
                totals(participant,2) = totals(participant,2) + rating(i);
            end

            if(strcmp('ach',algo(i)) == 1)
                totals(participant,3) = totals(participant,3) + rating(i);
            end
        end
        
        %get the mean ratings for each algorithm for each participant
        for i=1:length(data{1})
            if(strcmp('wgw',algo(i)) == 1)
                means(participant,1) = totals(participant,1)/6;
            end

            if(strcmp('sdn',algo(i)) == 1)
                means(participant,2) = totals(participant,2)/6;
            end

            if(strcmp('ach',algo(i)) == 1)
                means(participant,3) = totals(participant,3)/6;
            end
        end
        
        %get the total ratings for specifically ball and speech sounds
        for i=1:length(data{1})
            if(strcmp('speech',sound(i)) == 1)
                if(strcmp('wgw',algo(i)) == 1)
                    speech(participant,1) = speech(participant,1) + rating(i);
                end

                if(strcmp('sdn',algo(i)) == 1)
                    speech(participant,2) = speech(participant,2) + rating(i);
                end

                if(strcmp('ach',algo(i)) == 1)
                    speech(participant,3) = speech(participant,3) + rating(i);
                end
            end
            
            if(strcmp('ball',sound(i)) == 1)
                if(strcmp('wgw',algo(i)) == 1)
                    ball(participant,1) = ball(participant,1) + rating(i);
                end

                if(strcmp('sdn',algo(i)) == 1)
                    ball(participant,2) = ball(participant,2) + rating(i);
                end

                if(strcmp('ach',algo(i)) == 1)
                    ball(participant,3) = ball(participant,3) + rating(i);
                end
            end
        end
        
        for i=1:length(data{1})
            sb(participant,1) = sum(speech(participant,:));
            sb(participant,2) = sum(ball(participant,:));
            
            sbMeans(participant,1) = sum(speech(participant,:))/9;
            sbMeans(participant,2) = sum(ball(participant,:))/9;
        end
        
        for i=1:length(data{1})
            if(pos(i) == 1)
               posTotals(participant,1) = posTotals(participant,1) + rating(i);
            end
 
            if(pos(i) == 2)
                posTotals(participant,2) = posTotals(participant,2) + rating(i);
            end
            
            if(pos(i) == 3)
                posTotals(participant,3) = posTotals(participant,3) + rating(i);
            end
        end
        
        for i=1:length(data{1})
            posMeans(participant,1) = posTotals(participant,1)/6;
            posMeans(participant,2) = posTotals(participant,2)/6;
            posMeans(participant,3) = posTotals(participant,3)/6;
        end
   end
end


%% Descriptive Statistics
%Means of the total ratings
meanTotalWGW = mean(totals(:,1));
meanTotalSDN = mean(totals(:,2));
meanTotalACH = mean(totals(:,3));

%Standard Deviation of the total ratings
stdTotalWGW = std(totals(:,1));
stdTotalSDN = std(totals(:,2));
stdTotalACH = std(totals(:,3));

%Variance of the total ratings
varTotalWGW = var(totals(:,1));
varTotalSDN = var(totals(:,2));
varTotalACH = var(totals(:,3));

%Means of the mean ratings
meanMeansWGW = mean(means(:,1));
meanMeansSDN = mean(means(:,2));
meanMeansACH = mean(means(:,3));

%Standard Deviation of the mean ratings
stdMeansWGW = std(means(:,1));
stdMeansSDN = std(means(:,2));
stdMeansACH = std(means(:,3));

%Variance of the mean ratings
varMeansWGW = var(means(:,1));
varMeansSDN = var(means(:,2));
varMeansACH = var(means(:,3));

%Standard Error of the mean ratings
SEMeansWGW = stdMeansWGW/sqrt(numParticipants);
SEMeansSDN = stdMeansSDN/sqrt(numParticipants);
SEMeansACH = stdMeansACH/sqrt(numParticipants);

%Means of sound type dependent total ratings
speechMean = mean(sb(:,1));
ballMean = mean(sb(:,2));

%Means of position dependent total ratings
pos1Mean = mean(posTotals(:,1));
pos2Mean = mean(posTotals(:,2));
pos3Mean = mean(posTotals(:,3));

%Means of sound type dependent mean ratings
speechMM = mean(sbMeans(:,1));
ballMM = mean(sbMeans(:,2));
STDspeechMM = std(sbMeans(:,1));
STDballMM = std(sbMeans(:,2));

%Means of position dependent mean ratings
pos1MM = mean(posMeans(:,1));
pos2MM = mean(posMeans(:,2));
pos3MM = mean(posMeans(:,3));
STDpos1MM = std(posMeans(:,1));
STDpos2MM = std(posMeans(:,2));
STDpos3MM = std(posMeans(:,3));
%% Testing for normal distribution / parametric data
%We are working with interval data. 

figure(1);
edges = [0 2:0.75:8 10];
histogram(means(:,1),edges);
axis([0 10 0 8]);
title('WGW Means');
xlabel('Rating');
ylabel('Frequency');
set(gca,'FontSize',14)

figure(2);
edges = [0 3:0.75:9 10];
histogram(means(:,2),edges);
axis([0 10 0 8]);
title('SDN Means');
xlabel('Rating');
ylabel('Frequency');
set(gca,'FontSize',14)

figure(3);
edges = [0 0.5:0.75:8 10];
histogram(means(:,3),edges);
axis([0 10 0 8]);
title('Anechoic Means');
xlabel('Rating');
ylabel('Frequency');
set(gca,'FontSize',14)

figure(4);
boxplot(means)
title('Boxplot of the mean ratings')
ylabel('Rating from 0 to 10');
xticklabels({'WGW', 'SDN','Anechoic'});
set(gca,'FontSize',14)

figure(5);
boxplot(ball/3)
title('Boxplot of the mean ratings for the ball signal')
ylabel('Rating from 0 to 10');
xticklabels({'WGW', 'SDN','Anechoic'});
set(gca,'FontSize',14)

figure(6);
boxplot(speech/3)
title('Boxplot of the mean ratings for the speech signal')
ylabel('Rating from 0 to 10');
xticklabels({'WGW', 'SDN','Anechoic'});
set(gca,'FontSize',14)

figure(7);
boxplot(posMeans)
title('Boxplot of the mean ratings for different positions')
ylabel('Rating from 0 to 10');
xticklabels({'Pos 1', 'Pos 2','Pos 3'});
set(gca,'FontSize',14)

%Anderson-Darling test for normal distribution
hWGW = adtest(means(:,1));
hSDN = adtest(means(:,2));
hACH = adtest(means(:,3));
%% t-test
wgw = means(:,1);
sdn = means(:,2);
ach = means(:,3);

[h1 p1] = ttest(wgw,sdn);

[h2 p2] = ttest(wgw,ach);

[h3 p3] = ttest(sdn,ach);